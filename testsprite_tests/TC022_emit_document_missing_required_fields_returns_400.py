import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENTS_ENDPOINT = "/v1/documents"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30


def test_emit_document_missing_required_fields_returns_400():
    # Step 1: Authenticate to get accessToken
    login_resp = requests.post(
        BASE_URL + LOGIN_ENDPOINT,
        json=LOGIN_PAYLOAD,
        timeout=TIMEOUT
    )
    assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid or missing accessToken"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: POST /v1/documents with incomplete JSON body
    incomplete_document_body = {
        "documentType": "01"
        # missing serie, customerDocType, customerDocNumber, customerName, and items
    }

    doc_resp = requests.post(
        BASE_URL + DOCUMENTS_ENDPOINT,
        json=incomplete_document_body,
        headers=headers,
        timeout=TIMEOUT
    )

    # Expected: 400 Bad Request
    assert doc_resp.status_code == 400, f"Expected 400 Bad Request, got {doc_resp.status_code}"

    try:
        error_json = doc_resp.json()
    except Exception:
        error_json = None
    assert error_json is not None, "Response did not return JSON body"
    # The error is expected, but fields may vary. Check for presence of error or validation messages.
    error_keys = ['error', 'message', 'errors']
    assert any(key in error_json for key in error_keys), f"Response JSON does not contain error details keys {error_keys}"


test_emit_document_missing_required_fields_returns_400()