import dayjs from "dayjs";
import { TimePicker } from "@mui/x-date-pickers/TimePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";

type AppTimePickerProps = {
  label: string;
  value?: string | null;
  onChange: (value: string) => void;
  error?: boolean;
  helperText?: string;
  disabled?: boolean;
};

export function AppTimePicker({ label, value, onChange, error, helperText, disabled }: AppTimePickerProps) {
  const parsedValue = value ? dayjs(`2000-01-01T${value}:00`) : null;

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="th">
      <TimePicker
        label={label}
        value={parsedValue}
        onChange={(nextValue) => onChange(nextValue?.isValid() ? nextValue.format("HH:mm") : "")}
        ampm={false}
        format="HH:mm"
        minutesStep={1}
        disabled={disabled}
        slotProps={{
          textField: {
            fullWidth: true,
            required: true,
            error,
            helperText: helperText ?? "รูปแบบ 24 ชั่วโมง (ชั่วโมง:นาที)",
            InputLabelProps: { shrink: true },
          },
        }}
      />
    </LocalizationProvider>
  );
}
