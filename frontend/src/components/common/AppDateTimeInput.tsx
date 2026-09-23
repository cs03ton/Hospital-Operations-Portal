import { Stack, TextField, type TextFieldProps } from "@mui/material";
import { AppDatePicker } from "./AppDatePicker";

/** Compatibility with form event handlers; value is a Gregorian Bangkok wall time. */
export function AppDateTimeInput(props: TextFieldProps) {
  const { value, onChange, label, disabled, error, helperText, required, fullWidth, size } = props;
  const [date = "", time = ""] = String(value ?? "").split("T");
  const change = (next: string) => onChange?.({ target: { value: next, name: props.name ?? "" } } as React.ChangeEvent<HTMLInputElement>);
  return <Stack direction={{ xs: "column", sm: "row" }} spacing={1} sx={{ width: fullWidth ? "100%" : undefined }}>
    <AppDatePicker label={String(label ?? "วันที่")} value={date} disabled={disabled} error={error} required={required}
      helperText={typeof helperText === "string" ? helperText : undefined} size={size}
      onChange={next => change(next ? `${next}T${time || "00:00"}` : "")} />
    <TextField label="เวลา (ประเทศไทย)" type="time" value={time} disabled={disabled || !date}
      required={required} size={size ?? "small"} error={error} InputLabelProps={{ shrink: true }}
      onChange={event => change(`${date}T${event.target.value}`)} />
  </Stack>;
}
