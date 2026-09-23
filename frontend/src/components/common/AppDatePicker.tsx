import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import type { Dayjs } from "dayjs";
import dayjs from "dayjs";
import buddhistEra from "dayjs/plugin/buddhistEra";
import "dayjs/locale/th";
import { normalizeBuddhistApiDate } from "../../utils/dateFormat";

dayjs.extend(buddhistEra);

export class AdapterDayjsBuddhist extends AdapterDayjs {
  constructor(options?: ConstructorParameters<typeof AdapterDayjs>[0]) {
    super(options);

    this.formatTokenMap = {
      ...this.formatTokenMap,
      BBBB: {
        sectionType: "year",
        contentType: "digit",
        maxLength: 4,
      },
    };

    const baseParse = this.parse;
    this.parse = (value, format) => {
      if (!format.includes("BBBB")) {
        return baseParse(value, format);
      }

      const normalizedFormat = format.replace(/BBBB/g, "YYYY");
      const normalizedValue = value.replace(/\d{4}(?!.*\d{4})/, (yearText) => {
        const year = Number(yearText);
        return year >= 2443 && year <= 2643 ? String(year - 543) : yearText;
      });

      return baseParse(normalizedValue, normalizedFormat);
    };
  }
}

type AppDatePickerProps = {
  label: string;
  value?: string | null;
  onChange: (value: string) => void;
  error?: boolean;
  helperText?: string;
  fullWidth?: boolean;
  size?: "small" | "medium";
  disabled?: boolean;
  required?: boolean;
  buddhistYear?: boolean;
};

export function AppDatePicker({
  label,
  value,
  onChange,
  error,
  helperText,
  fullWidth = true,
  size = "small",
  disabled = false,
  required = false,
  buddhistYear = true,
}: AppDatePickerProps) {
  function handleChange(nextValue: Dayjs | null) {
    const formatted = nextValue?.isValid() ? nextValue.format("YYYY-MM-DD") : "";
    onChange(buddhistYear ? normalizeBuddhistApiDate(formatted) : formatted);
  }

  return (
    <LocalizationProvider
      dateAdapter={buddhistYear ? AdapterDayjsBuddhist : AdapterDayjs}
      adapterLocale="th"
      dateFormats={buddhistYear ? {
        year: "BBBB",
        fullDate: "DD MMMM BBBB",
        keyboardDate: "DD/MM/BBBB",
        normalDate: "D MMMM",
        normalDateWithWeekday: "dd, D MMMM",
      } : undefined}
      localeText={{
        cancelButtonLabel: "ยกเลิก",
        clearButtonLabel: "ล้าง",
        okButtonLabel: "ตกลง",
        todayButtonLabel: "วันนี้",
        datePickerToolbarTitle: "เลือกวันที่",
      }}
    >
      <DatePicker
        label={label}
        value={value ? dayjs(buddhistYear ? normalizeBuddhistApiDate(value) : value) : null}
        onChange={handleChange}
        disabled={disabled}
        minDate={dayjs("1900-01-01")}
        maxDate={dayjs("2100-12-31")}
        format={buddhistYear ? "DD/MM/BBBB" : "DD/MM/YYYY"}
        slotProps={{
          textField: {
            fullWidth,
            required,
            size,
            error,
            helperText,
            InputLabelProps: { shrink: true },
          },
        }}
      />
    </LocalizationProvider>
  );
}
