from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from app.api.analytics import router as analytics_router
from app.api.chat import router as chat_router
from app.config import ConfigurationError

app = FastAPI(title="Classly Agents")
app.include_router(chat_router)
app.include_router(analytics_router)


@app.exception_handler(ConfigurationError)
def configuration_error_handler(request: Request, error: ConfigurationError) -> JSONResponse:
    return JSONResponse(status_code=503, content={"detail": "The agent is not configured"})


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}
