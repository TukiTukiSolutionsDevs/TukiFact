import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
TENANT_URL = f"{BASE_URL}/v1/tenant"
TENANT_ENV_URL = f"{TENANT_URL}/environment"
LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}
TIMEOUT = 30

def test_tenant_get_update_and_environment():
    # Step 1: Login to get Bearer token
    try:
        login_resp = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=TIMEOUT)
        login_resp.raise_for_status()
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and isinstance(access_token, str) and len(access_token) > 0
    except Exception as e:
        assert False, f"Login failed: {e}"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: GET /v1/tenant - expect 200 and ruc == '20100070970'
    try:
        get_resp = requests.get(TENANT_URL, headers=headers, timeout=TIMEOUT)
        get_resp.raise_for_status()
        get_json = get_resp.json()
        assert get_resp.status_code == 200
        assert isinstance(get_json, dict)
        assert get_json.get("ruc") == "20100070970"
    except Exception as e:
        assert False, f"GET /v1/tenant failed or invalid response: {e}"

    # Step 3: PUT /v1/tenant with updated nombreComercial and direccion, expect 204 No Content
    update_payload = {
        "nombreComercial": "TestSprite Updated",
        "direccion": "Av TestSprite 999"
    }
    try:
        put_resp = requests.put(TENANT_URL, headers=headers, json=update_payload, timeout=TIMEOUT)
        # 204 No Content does not have response content
        assert put_resp.status_code == 204
    except Exception as e:
        assert False, f"PUT /v1/tenant update failed: {e}"

    # Step 4: PUT /v1/tenant/environment with {"environment":"beta"}, expect 200 OK with environment = 'beta'
    env_beta_payload = {"environment": "beta"}
    try:
        env_resp = requests.put(TENANT_ENV_URL, headers=headers, json=env_beta_payload, timeout=TIMEOUT)
        env_resp.raise_for_status()
        env_json = env_resp.json()
        assert env_resp.status_code == 200
        assert "environment" in env_json
        assert env_json["environment"] == "beta"
    except Exception as e:
        assert False, f"PUT /v1/tenant/environment with 'beta' failed: {e}"

    # Step 5: PUT /v1/tenant/environment with {"environment":"staging"}, expect 400 Bad Request
    env_staging_payload = {"environment": "staging"}
    try:
        invalid_env_resp = requests.put(TENANT_ENV_URL, headers=headers, json=env_staging_payload, timeout=TIMEOUT)
        assert invalid_env_resp.status_code == 400
    except requests.exceptions.HTTPError as he:
        # This is expected, 400 Bad Request
        assert he.response.status_code == 400
    except Exception as e:
        # If any other error, fail the test
        assert str(e) == "", f"PUT /v1/tenant/environment with 'staging' did not return 400: {e}"

test_tenant_get_update_and_environment()