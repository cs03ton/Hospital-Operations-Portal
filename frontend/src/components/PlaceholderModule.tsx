import { Card, CardContent, Stack, Typography } from "@mui/material";
import { PageHeader } from "./PageHeader";

type PlaceholderModuleProps = {
  title: string;
  subtitle: string;
  items: string[];
};

export function PlaceholderModule({ title, subtitle, items }: PlaceholderModuleProps) {
  return (
    <>
      <PageHeader title={title} subtitle={subtitle} />
      <Card>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>
            โครงสร้างที่เตรียมไว้
          </Typography>
          <Stack spacing={1}>
            {items.map((item) => (
              <Typography key={item} color="text.secondary">
                {item}
              </Typography>
            ))}
          </Stack>
        </CardContent>
      </Card>
    </>
  );
}
