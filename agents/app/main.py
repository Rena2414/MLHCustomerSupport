from fastapi import FastAPI

app = FastAPI(title="MlhSupport Agents")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}
