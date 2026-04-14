import requests

def test_backoffice_get_tenant_detail():
    base_url = "http://localhost:80"
    backoffice_login_url = f"{base_url}/v1/backoffice/auth/login"
    tenant_id = "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    tenant_detail_url = f"{base_url}/v1/backoffice/tenants/{tenant_id}"
    timeout = 30

    # Authenticate as backoffice superadmin to get token
    login_payload = {
        "email": "superadmin@tukifact.net.pe",
        "password": "SuperAdmin2026!"
    }
    try:
        login_resp = requests.post(backoffice_login_url, json=login_payload, timeout=timeout)
        login_resp.raise_for_status()
    except Exception as e:
        assert False, f"Backoffice login request failed: {e}"
    login_data = login_resp.json()
    access_token = login_data.get("accessToken")
    assert access_token and access_token.startswith("eyJ"), "Invalid access token received from backoffice login"
    user = login_data.get("user")
    assert user and user.get("role") == "superadmin", "User role is not superadmin"

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Get tenant detail using backoffice token and tenant ID
    try:
        resp = requests.get(tenant_detail_url, headers=headers, timeout=timeout)
        resp.raise_for_status()
    except Exception as e:
        assert False, f"Request to get tenant detail failed: {e}"

    assert resp.status_code == 200, f"Expected status code 200, got {resp.status_code}"

    tenant_data = resp.json()

    # Validate required fields in response JSON
    assert isinstance(tenant_data, dict), "Response is not a JSON object"

    # Check top-level required fields
    for field in ['ruc', 'razonSocial', 'isActive', 'plan', 'users', 'stats']:
        assert field in tenant_data, f"Missing '{field}' in tenant detail response"

    # Validate 'ruc' is non-empty string
    assert isinstance(tenant_data['ruc'], str) and tenant_data['ruc'], "'ruc' should be a non-empty string"

    # Validate 'razonSocial' is non-empty string
    assert isinstance(tenant_data['razonSocial'], str) and tenant_data['razonSocial'], "'razonSocial' should be a non-empty string"

    # Validate 'isActive' is boolean
    assert isinstance(tenant_data['isActive'], bool), "'isActive' should be a boolean"

    # Validate 'plan' is an object (dict)
    plan = tenant_data['plan']
    assert isinstance(plan, dict), "'plan' should be an object"

    # Validate 'users' is an array with at least 1 element
    users = tenant_data['users']
    assert isinstance(users, list), "'users' should be a list"
    assert len(users) >= 1, "'users' array should have at least one user"

    # Validate 'stats' is an object with 'totalDocuments' and 'monthDocuments'
    stats = tenant_data['stats']
    assert isinstance(stats, dict), "'stats' should be an object"
    for stat_field in ['totalDocuments', 'monthDocuments']:
        assert stat_field in stats, f"Missing '{stat_field}' in 'stats'"
        # Validate that totalDocuments and monthDocuments are numbers (int or float) and non-negative
        val = stats[stat_field]
        assert isinstance(val, (int, float)), f"'{stat_field}' should be a number"
        assert val >= 0, f"'{stat_field}' should be >= 0"

test_backoffice_get_tenant_detail()