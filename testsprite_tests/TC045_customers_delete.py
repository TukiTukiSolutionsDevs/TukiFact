import requests

def test_customers_delete():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    customer_id = "a7d9acea-8eb2-4832-85ba-72afe9d99a69"  # ID from TC040 (María)

    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    try:
        login_resp = requests.post(login_url, json=login_payload, timeout=30)
        login_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Login failed: {e}"

    token = login_resp.json().get("accessToken")
    assert token and token.startswith("eyJ"), "Invalid accessToken received."

    headers = {
        "Authorization": f"Bearer {token}"
    }

    delete_url = f"{base_url}/v1/customers/{customer_id}"
    try:
        delete_resp = requests.delete(delete_url, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"DELETE request failed: {e}"

    # According to notes, DELETE on customers returns 204 or 400 if has associated docs.
    # Expected per test case: 200 OK with 'message' containing 'eliminado'.
    # But notes say 204 expected; respect test case and check for message or 204.
    if delete_resp.status_code == 200:
        json_del = delete_resp.json()
        message = json_del.get("message", "").lower()
        assert "eliminado" in message, f"Expected 'eliminado' in message, got: {json_del.get('message')}"
    else:
        # Accept 204 No Content as well
        assert delete_resp.status_code == 204, f"Unexpected delete status code: {delete_resp.status_code}"

    get_url = f"{base_url}/v1/customers/{customer_id}"
    try:
        get_resp = requests.get(get_url, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"GET request failed: {e}"

    assert get_resp.status_code == 404, f"Expected 404 after deletion, got {get_resp.status_code}"

test_customers_delete()