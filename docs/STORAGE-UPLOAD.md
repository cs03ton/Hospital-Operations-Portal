# Storage Upload

Leave attachments are stored outside source code in a configured storage path.

## Environment

```text
STORAGE_ROOT_PATH=./storage
LEAVE_ATTACHMENT_MAX_FILE_SIZE_MB=5
LEAVE_ATTACHMENT_ALLOWED_EXTENSIONS=.pdf,.jpg,.jpeg,.png
```

## Structure

```text
storage/
├── leave-attachments/
│   └── yyyy/
│       └── mm/
│           └── leave-request-id/
└── profile-images/
    └── user-id/
        └── avatar.{jpg|png|webp}
```

## Rules

- Allowed extensions: pdf, jpg, jpeg, png
- Max size comes from configuration
- Stored file names are generated server-side
- Original file names are retained in database metadata

## Test Upload

```powershell
curl.exe -X POST "http://localhost:5000/api/leave-requests/<id>/attachments" `
  -H "Authorization: Bearer <token>" `
  -F "file=@C:\Temp\sample.pdf;type=application/pdf"
```

## Download

```powershell
curl.exe -L "http://localhost:5000/api/leave-attachments/<attachment-id>/download" `
  -H "Authorization: Bearer <token>" `
  -o sample.pdf
```

Download access requires `LeaveAttachment.Download` and is limited to the request owner, assigned approver, approval history approver, or leave manager.

## Profile Images

Profile images are uploaded from `ข้อมูลส่วนตัวของฉัน`.

- Path: `storage/profile-images/{userId}/avatar.{ext}`
- Allowed types: JPG, PNG, WEBP
- Max size: 2 MB
- New uploads replace the previous avatar file

The backend returns `profileImageUrl` with cache busting so Header/avatar UI refreshes immediately after upload.
