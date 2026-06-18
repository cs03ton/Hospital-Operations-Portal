# Branding

Phase 1.2 uses the hospital logo as the primary brand identity.

## Logo

Primary logo:

```text
frontend/src/assets/logo/hospital-logo.png
```

The logo is displayed on:

- Login page
- Sidebar
- Header
- Browser favicon

## Logo Colors

Logo color sampling found the primary green:

```text
Primary:   #056839
Secondary: #126D3F
Accent:    #F4FFE0
Border:    #DCE7DD
Background:#F6FAF4
```

Material UI theme is defined in:

```text
frontend/src/theme/theme.ts
```

## UI Rules

- Keep logo aspect ratio.
- Do not recolor or crop the logo.
- Thai is the default UI language.
- Source code, database names, and API endpoints remain English.

## Future Multi-Language Readiness

Current UI text is Thai-first. Future localization should introduce a translation layer for `th-TH` and `en-US` without changing route names or API contracts.
