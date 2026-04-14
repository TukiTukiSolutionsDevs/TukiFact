import requests

def test_auth_register_missing_required_fields_returns_400():
    base_url = "http://localhost:80"
    url = f"{base_url}/v1/auth/register"
    payload = {
        "ruc": "20111222333"
        # missing razonSocial, adminEmail, adminPassword, adminFullName
    }
    headers = {
        "Content-Type": "application/json"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 400, f"Expected 400 Bad Request but got {response.status_code}"
    # Optionally verify error message in response body if present
    try:
        data = response.json()
        assert "error" in data or "message" in data or "errors" in data, "Expected error message in response body"
    except ValueError:
        # If response not JSON, ignore further checks
        pass

test_auth_register_missing_required_fields_returns_400()