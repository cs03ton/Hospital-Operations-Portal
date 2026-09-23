import React, { useState } from "react";
import { createRoot } from "react-dom/client";
import { AppDatePicker } from "../../src/components/common/AppDatePicker";
import { AppDateTimeInput } from "../../src/components/common/AppDateTimeInput";
import { bangkokInputToUtc, formatThaiDateTime } from "../../src/utils/dateFormat";

function Fixture() {
  const [date, setDate] = useState("2026-09-24");
  const [instant, setInstant] = useState("2026-09-24T08:00");
  return <><AppDatePicker label="วันที่ลา" value={date} onChange={setDate} />
    <output data-testid="date">{date}</output>
    <AppDateTimeInput label="วันเวลา" value={instant} onChange={event => setInstant(event.target.value)} />
    <output data-testid="instant">{instant ? bangkokInputToUtc(instant) : ""}</output>
    <p>{formatThaiDateTime("2026-09-23T18:00:00Z")}</p></>;
}
createRoot(document.getElementById("root")!).render(<Fixture />);
