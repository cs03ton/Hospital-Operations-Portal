import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import type { Dayjs } from "dayjs";
import dayjs from "dayjs";
import "dayjs/locale/th";
import { formatDateForApi } from "../../utils/dateFormat";

type AppDatePickerProps = {
  label: string;
  value?: string | null;
  onChange: (value: string) => void;
  error?: boolean;
  helperText?: string;
  fullWidth?: boolean;
  size?: "small" | "medium";
};

export function AppDatePicker({
  label,
  value,
  onChange,
  error,
  helperText,
  fullWidth = true,
  size = "small",
}: AppDatePickerProps) {
  function handleChange(nextValue: Dayjs | null) {
    onChange(nextValue?.isValid() ? formatDateForApi(nextValue.toDate()) : "");
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="th">
      <DatePicker
        label={label}
        value={value ? dayjs(value) : null}
        onChange={handleChange}
        format="DD/MM/YYYY"
        slotProps={{
          textField: {
            fullWidth,
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
