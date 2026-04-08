import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
SERIES_URL = f"{BASE_URL}/v1/series"
AUTH_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}
TIMEOUT = 30

def test_post_v1_series_create_new_document_series():
    # Authenticate as admin to get token
    login_resp = requests.post(
        LOGIN_URL,
        json=AUTH_PAYLOAD,
        timeout=TIMEOUT
    )
    assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
    token = login_resp.json().get("accessToken")
    assert token and isinstance(token, str), "No accessToken in login response"

    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }

    # Use a new unique serie code "F003" per CRITICAL RULES, with documentType "01" and emissionPoint "0001"
    serie_code = "F003"
    payload = {
        "documentType": "01",
        "serie": serie_code,
        "emissionPoint": "0001"
    }

    resp = requests.post(SERIES_URL, json=payload, headers=headers, timeout=TIMEOUT)
    assert resp.status_code == 201, f"Series creation failed: {resp.status_code} {resp.text}"
    data = resp.json()
    # Validate response includes created series fields and currentCorrelative
    assert data.get("serie") == serie_code, "Returned serie mismatch"
    assert data.get("documentType") == "01", "Returned documentType mismatch"
    # currentCorrelative must be present and be a number (likely int)
    current_correlative = data.get("currentCorrelative")
    assert current_correlative is not None, "currentCorrelative missing in response"
    assert isinstance(current_correlative, int), "currentCorrelative is not int"


test_post_v1_series_create_new_document_series()