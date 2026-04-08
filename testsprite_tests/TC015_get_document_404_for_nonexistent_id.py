import requests

BASE_URL = "http://localhost:5186"

def test_get_document_404_for_nonexistent_id():
    login_url = f"{BASE_URL}/v1/auth/login"
    document_id = "00000000-0000-0000-0000-000000000000"
    document_url = f"{BASE_URL}/v1/documents/{document_id}"
    login_payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    headers = {"Content-Type": "application/json"}

    try:
        # Login to obtain Bearer token
        login_resp = requests.post(login_url, json=login_payload, headers=headers, timeout=30)
        login_resp.raise_for_status()
        access_token = login_resp.json().get("accessToken")
        assert access_token and isinstance(access_token, str), "Login failed to return valid accessToken"

        # Prepare Authorization header
        auth_headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Attempt to GET a document with a nonexistent ID, expecting 404
        resp = requests.get(document_url, headers=auth_headers, timeout=30)

        assert resp.status_code == 404, f"Expected 404 Not Found, got {resp.status_code}"
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

test_get_document_404_for_nonexistent_id()