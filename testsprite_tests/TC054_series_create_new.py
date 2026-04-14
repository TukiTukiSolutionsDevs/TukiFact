import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
SERIES_URL = f"{BASE_URL}/v1/series"
TIMEOUT = 30

def test_series_create_new():
    # Step 1: Authenticate tenant user to get access token
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"
    assert login_resp.status_code == 200, f"Login failed: {login_resp.status_code} {login_resp.text}"

    login_json = login_resp.json()
    access_token = login_json.get('accessToken')
    assert access_token and isinstance(access_token, str) and access_token.startswith('eyJ'), "Invalid accessToken in login response"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: POST /v1/series to create a new series
    series_payload = {
        "documentType": "01",
        "serie": "FZ77",
        "emissionPoint": "0001"
    }

    series_id = None
    try:
        response = requests.post(SERIES_URL, headers=headers, json=series_payload, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Series creation request failed: {e}"

    assert response.status_code == 201, f"Expected 201 Created, got {response.status_code}, {response.text}"

    try:
        series_json = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    # Validate response JSON fields
    series_id = series_json.get("id")
    assert series_id and isinstance(series_id, str), "Missing or invalid 'id' in response"
    assert series_json.get("serie") == "FZ77", f"Expected serie 'FZ77', got {series_json.get('serie')}"
    assert series_json.get("documentType") == "01", f"Expected documentType '01', got {series_json.get('documentType')}"
    assert series_json.get("currentCorrelative") == 0, f"Expected currentCorrelative 0, got {series_json.get('currentCorrelative')}"
    assert series_json.get("isActive") is True, f"Expected isActive true, got {series_json.get('isActive')}"

    # No cleanup delete endpoint specified in PRD for series, so leave created data

test_series_create_new()