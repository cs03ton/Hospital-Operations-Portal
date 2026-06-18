# HOP Deployment Notes

This folder contains the initial deployment foundation for Hospital Operations Portal.

## Files

- `backend.Dockerfile` builds and runs the .NET 9 API.
- `frontend.Dockerfile` builds the React app and serves it with Nginx.
- `nginx.conf` proxies frontend traffic and API traffic.

## Local Docker Run

From the project root:

```bash
docker compose up --build
```

Open:

- Frontend: http://localhost:5173
- API: http://localhost:5000/api
- Health check: http://localhost:5000/healthz
- Nginx gateway: http://localhost:8080
