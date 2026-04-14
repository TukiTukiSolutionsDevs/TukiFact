import requests

BASE_URL = "http://localhost:80"
BACKOFFICE_AUTH_ENDPOINT = "/v1/backoffice/auth/login"
BACKOFFICE_CONFIG_ENDPOINT = "/v1/backoffice/config"
BACKOFFICE_EMAIL = "superadmin@tukifact.net.pe"
BACKOFFICE_PASSWORD = "SuperAdmin2026!"
TIMEOUT = 30

def test_backoffice_config_get_and_update():
    # Authenticate to get BACKOFFICE_TOKEN
    auth_payload = {
        "email": BACKOFFICE_EMAIL,
        "password": BACKOFFICE_PASSWORD
    }
    try:
        auth_resp = requests.post(
            f"{BASE_URL}{BACKOFFICE_AUTH_ENDPOINT}",
            json=auth_payload,
            timeout=TIMEOUT
        )
        auth_resp.raise_for_status()
    except Exception as e:
        assert False, f"Backoffice auth login failed: {e}"
    auth_data = auth_resp.json()
    access_token = auth_data.get("accessToken")
    assert access_token and access_token.startswith("eyJ"), "Invalid backoffice access token"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 1: GET /v1/backoffice/config and verify required fields exist
    try:
        get_resp = requests.get(
            f"{BASE_URL}{BACKOFFICE_CONFIG_ENDPOINT}",
            headers=headers,
            timeout=TIMEOUT
        )
        get_resp.raise_for_status()
    except Exception as e:
        assert False, f"GET backoffice config failed: {e}"
    config_data = get_resp.json()
    expected_fields = [
        'maintenance_mode',
        'registration_enabled',
        'default_plan',
        'max_free_documents',
        'trial_days',
        'sunat_beta_mode',
        'email_provider',
        'support_email'
    ]
    for field in expected_fields:
        assert field in config_data, f"'{field}' missing in config response"

    # Step 2: PUT /v1/backoffice/config with updated fields
    update_payload = {
        "trial_days": "30",
        "support_email": "soporte-prd@tukifact.net.pe"
    }
    try:
        put_resp = requests.put(
            f"{BASE_URL}{BACKOFFICE_CONFIG_ENDPOINT}",
            headers=headers,
            json=update_payload,
            timeout=TIMEOUT
        )
        put_resp.raise_for_status()
    except Exception as e:
        assert False, f"PUT backoffice config failed: {e}"
    put_data = put_resp.json()
    message = put_data.get("message", "").lower()
    assert "actualizada" in message, "PUT response message does not contain 'actualizada'"

    # Step 3: GET /v1/backoffice/config again to verify 'trial_days' update
    try:
        get_after_put_resp = requests.get(
            f"{BASE_URL}{BACKOFFICE_CONFIG_ENDPOINT}",
            headers=headers,
            timeout=TIMEOUT
        )
        get_after_put_resp.raise_for_status()
    except Exception as e:
        assert False, f"GET backoffice config after PUT failed: {e}"
    updated_config = get_after_put_resp.json()
    assert updated_config.get("trial_days") == "30", f"Expected 'trial_days' to be '30', got: {updated_config.get('trial_days')}"

test_backoffice_config_get_and_update()