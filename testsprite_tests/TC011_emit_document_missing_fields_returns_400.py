import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENTS_URL = f"{BASE_URL}/v1/documents"

LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}

TIMEOUT = 30


def test_emit_document_missing_fields_returns_400():
    # Step 1: Login and obtain Bearer token
    login_headers = {"Content-Type": "application/json"}
    login_response = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, headers=login_headers, timeout=TIMEOUT)
    assert login_response.status_code == 200, f"Login failed with status {login_response.status_code}"
    login_data = login_response.json()
    access_token = login_data.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.strip() != "", "No valid accessToken in login response"

    # Step 2: Send incomplete document payload (missing required fields)
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {access_token}"
    }
    incomplete_payload = {"documentType": "01"}

    response = requests.post(DOCUMENTS_URL, json=incomplete_payload, headers=headers, timeout=TIMEOUT)

    # Step 3: Verify response is 400 Bad Request
    assert response.status_code == 400, f"Expected 400 Bad Request, got {response.status_code}"

    # Optionally check error message or details indicate the missing fields
    try:
        error_data = response.json()
        # Check if error message mentions missing fields (not required, but helpful)
        missing_fields = ["serie", "customerDocType", "customerDocNumber", "customerName", "items"]
        error_message = str(error_data).lower()
        for field in missing_fields:
            assert field.lower() in error_message or field.lower() in str(error_data.get("errors", "")).lower(), \
                f"Error response does not mention missing field: {field}"
    except Exception:
        # If JSON decode fails, just pass as main check is status code
        pass


test_emit_document_missing_fields_returns_400()