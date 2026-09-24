import { useEffect, useState } from "react";
import { Box } from "@mui/material";
import { httpClient } from "../api/httpClient";
import defaultRoom from "../assets/meeting-room-default.svg";

export function MeetingRoomImage({ photoUrl, alt, height = 145 }: { photoUrl?: string | null; alt: string; height?: number }) {
  const [source, setSource] = useState(defaultRoom);
  useEffect(() => {
    if (!photoUrl) { setSource(defaultRoom); return; }
    let active = true;
    let objectUrl = "";
    httpClient.get<Blob>(photoUrl, { responseType: "blob" }).then((response) => {
      if (!active) return;
      objectUrl = URL.createObjectURL(response.data);
      setSource(objectUrl);
    }).catch(() => { if (active) setSource(defaultRoom); });
    return () => { active = false; if (objectUrl) URL.revokeObjectURL(objectUrl); };
  }, [photoUrl]);
  return <Box component="img" src={source} alt={alt} sx={{ width: "100%", height, objectFit: "cover", bgcolor: "#eff4f0" }} />;
}
