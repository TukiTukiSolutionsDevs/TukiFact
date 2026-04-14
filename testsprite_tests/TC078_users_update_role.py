import requests

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
USER_ENDPOINT_TEMPLATE = "/v1/users/{user_id}"

def test_users_update_role():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    # User ID from TC075
    user_id = "38e5595f-b788-4496-9593-70003b23da3c"

    try:
        # Authenticate to get access token
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=login_payload,
            timeout=30
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        update_payload = {"role": "consulta"}
        put_resp = requests.put(
            BASE_URL + USER_ENDPOINT_TEMPLATE.format(user_id=user_id),
            json=update_payload,
            headers=headers,
            timeout=30
        )
        assert put_resp.status_code == 204, f"Expected 204 No Content but got {put_resp.status_code}"

    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

test_users_update_role()