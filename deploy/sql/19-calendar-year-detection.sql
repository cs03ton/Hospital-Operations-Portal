-- HOP full calendar-year detection (read-only)
-- Run in DBeaver with Execute SQL Script. This script never updates application data.
ROLLBACK;
BEGIN TRANSACTION READ ONLY;

CREATE TEMP TABLE hop_calendar_anomalies (
    table_name text, column_name text, primary_key text, data_type text,
    current_value text, proposed_value text, reason text
) ON COMMIT DROP;

DO $audit$
DECLARE c record; pk_expression text;
BEGIN
  FOR c IN
    SELECT cols.table_schema, cols.table_name, cols.column_name, cols.data_type
    FROM information_schema.columns cols
    WHERE cols.table_schema = 'public'
      AND cols.data_type IN ('date', 'timestamp without time zone', 'timestamp with time zone')
  LOOP
    SELECT string_agg(format('%I::text', a.attname), ' || ''|'' || ' ORDER BY x.ordinality)
      INTO pk_expression
    FROM pg_index i
    CROSS JOIN LATERAL unnest(i.indkey) WITH ORDINALITY x(attnum, ordinality)
    JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = x.attnum
    WHERE i.indrelid = format('%I.%I', c.table_schema, c.table_name)::regclass AND i.indisprimary;
    pk_expression := coalesce(pk_expression, '''(no primary key)''');

    EXECUTE format(
      'INSERT INTO hop_calendar_anomalies
       SELECT %L,%L,%s,%L,%I::text,(%I - interval ''543 years'')::text,
              ''calendar year outside CE 1900-2100; review before repair''
       FROM %I.%I WHERE extract(year from %I) BETWEEN 2443 AND 2643',
      c.table_name,c.column_name,pk_expression,c.data_type,c.column_name,c.column_name,
      c.table_schema,c.table_name,c.column_name);
  END LOOP;
END $audit$;

SELECT table_name, column_name, count(*) AS affected_rows
FROM hop_calendar_anomalies GROUP BY table_name, column_name ORDER BY 1,2;
SELECT * FROM hop_calendar_anomalies ORDER BY table_name, column_name, primary_key;

-- Integer years are semantic fields. Fiscal/business years must be reviewed and are
-- intentionally NOT given an automatic proposed value.
SELECT table_name, column_name, data_type
FROM information_schema.columns
WHERE table_schema='public'
  AND data_type IN ('smallint','integer','bigint')
  AND column_name ~* '(^|_)(year|fiscal_year|manufacture_year)$'
ORDER BY table_name,column_name;

-- Text fields whose name suggests a date may contain legacy date strings.
CREATE TEMP TABLE hop_text_date_columns AS
SELECT table_schema,table_name,column_name
FROM information_schema.columns
WHERE table_schema='public' AND data_type IN ('text','character varying')
  AND column_name ~* '(date|time|year|expiry|expired|effective)';
SELECT * FROM hop_text_date_columns ORDER BY table_name,column_name;

-- Leave-specific reconciliation preview. It does not change balances.
WITH days AS (
  SELECT r.id,r.request_number,r.user_id,r.leave_type_id,r.status,r.start_date,r.end_date,
         r.duration_type,r.total_days,
         count(*) FILTER (WHERE extract(isodow from d)::int <= 5 AND h.id IS NULL)::numeric AS work_days
  FROM leave_requests r
  CROSS JOIN LATERAL generate_series(r.start_date::timestamp,r.end_date::timestamp,interval '1 day') d
  LEFT JOIN leave_holidays h ON h.holiday_date=d::date AND h.is_active
  WHERE extract(year from r.start_date) BETWEEN 1900 AND 2643
    AND extract(year from r.end_date) BETWEEN 1900 AND 2643
    AND r.end_date>=r.start_date AND r.end_date-r.start_date<=366
  GROUP BY r.id
), expected AS (
  SELECT *,CASE WHEN duration_type IN ('HALF_DAY_AM','HALF_DAY_PM')
                THEN CASE WHEN start_date=end_date AND work_days=1 THEN 0.5 ELSE 0 END
                ELSE work_days END AS proposed_days
  FROM days
)
SELECT *,CASE WHEN extract(year from start_date)>2100 OR extract(year from end_date)>2100
              THEN 'FIX_DATE_FIRST' WHEN total_days<>proposed_days THEN 'REVIEW_DAY_COUNT' END AS finding
FROM expected
WHERE extract(year from start_date)>2100 OR extract(year from end_date)>2100 OR total_days<>proposed_days
ORDER BY start_date,request_number;

COMMIT;
