import requests
import uuid
import time
from datetime import datetime

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
VOIDED_DOCUMENTS_ENDPOINT = "/v1/voided-documents"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30


def is_valid_uuid(val):
    try:
        uuid.UUID(str(val))
        return True
    except Exception:
        return False


def is_iso_date(s):
    # Check if s is a valid ISO 8601 date string by trying to parse it with fromisoformat
    try:
        # fromisoformat supports only limited ISO8601 subset, so fallback else to datetime.strptime with %Y-%m-%d %H:%M:%S
        datetime.fromisoformat(s.replace("Z", "+00:00"))
        return True
    except Exception:
        return False


def test_list_voided_documents():
    # Step 1: Authenticate and obtain access token
    login_url = BASE_URL + LOGIN_ENDPOINT
    try:
        login_resp = requests.post(login_url, json=LOGIN_PAYLOAD, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"

    assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code} and body {login_resp.text}"
    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token is not None and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid or missing accessToken in login response"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Accept": "application/json"
    }

    # Step 2: GET /v1/voided-documents with Bearer token
    voided_docs_url = BASE_URL + VOIDED_DOCUMENTS_ENDPOINT
    try:
        voided_resp = requests.get(voided_docs_url, headers=headers, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"GET voided documents request failed: {e}"

    assert voided_resp.status_code == 200, f"Expected 200 OK but got {voided_resp.status_code} with body {voided_resp.text}"

    # The description says: GET /v1/voided-documents returns JSON array
    # Validate JSON array structure - list of dicts each with required fields
    try:
        voided_json = voided_resp.json()
    except Exception as e:
        assert False, f"Response is not valid JSON: {e}"

    # The API PRD mentions common patterns that List endpoints return {"data": [...], ...}
    # However test case states "200 OK with JSON array" - so accept either array or {"data": [...]}
    data = None
    if isinstance(voided_json, dict) and "data" in voided_json and isinstance(voided_json["data"], list):
        data = voided_json["data"]
    elif isinstance(voided_json, list):
        data = voided_json
    else:
        assert False, "Response JSON is not an array nor does it contain a 'data' array."

    # Must have at least 1 item (from TC030)
    assert isinstance(data, list) and len(data) >= 1, "Voided documents array must have at least 1 item"

    for item in data:
        # Each item must have 'id' (UUID), 'ticketNumber' (string starting with 'RA-'), 'status' (string), 'createdAt' (ISO date string)
        assert isinstance(item, dict), f"Document item is not an object: {item}"
        id_val = item.get("id")
        ticket_number = item.get("ticketNumber")
        status_val = item.get("status")
        created_at = item.get("createdAt")

        assert id_val is not None and is_valid_uuid(id_val), f"Invalid or missing 'id': {id_val}"
        assert isinstance(ticket_number, str) and ticket_number.startswith("RA-"), f"'ticketNumber' missing or does not start with 'RA-': {ticket_number}"
        assert isinstance(status_val, str) and status_val != "", "'status' missing or empty"
        assert isinstance(created_at, str) and is_iso_date(created_at), f"'createdAt' missing or not valid ISO date: {created_at}"


test_list_voided_documents()