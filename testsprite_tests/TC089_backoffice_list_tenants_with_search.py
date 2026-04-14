import requests

BASE_URL = "http://localhost:80"
BACKOFFICE_AUTH_URL = f"{BASE_URL}/v1/backoffice/auth/login"
BACKOFFICE_TENANTS_URL = f"{BASE_URL}/v1/backoffice/tenants"

BACKOFFICE_EMAIL = "superadmin@tukifact.net.pe"
BACKOFFICE_PASSWORD = "SuperAdmin2026!"

def test_backoffice_list_tenants_with_search():
    # Step 1: Authenticate at backoffice to get token
    auth_payload = {
        "email": BACKOFFICE_EMAIL,
        "password": BACKOFFICE_PASSWORD
    }
    try:
        auth_resp = requests.post(BACKOFFICE_AUTH_URL, json=auth_payload, timeout=30)
        assert auth_resp.status_code == 200, f"Backoffice auth failed with status {auth_resp.status_code}"
        auth_json = auth_resp.json()
        access_token = auth_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken in auth response"
        user = auth_json.get("user")
        assert user and user.get("role") == "superadmin", "User role not superadmin"
    except Exception as e:
        raise AssertionError(f"Authentication step failed: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Step 2: GET /v1/backoffice/tenants?page=1&pageSize=10
    params_page = {
        "page": 1,
        "pageSize": 10
    }
    try:
        resp_page = requests.get(BACKOFFICE_TENANTS_URL, headers=headers, params=params_page, timeout=30)
        assert resp_page.status_code == 200, f"Tenants list with pagination failed status {resp_page.status_code}"
        data_page = resp_page.json()
        tenants = data_page.get("data")
        pagination = data_page.get("pagination")
        assert isinstance(tenants, list), "'data' is not a list"
        assert isinstance(pagination, dict), "'pagination' is not an object"

        # Each tenant has specified fields
        for tenant in tenants:
            assert isinstance(tenant.get("id"), str) and tenant["id"], "Tenant missing non-empty 'id'"
            assert isinstance(tenant.get("ruc"), str) and tenant["ruc"], "Tenant missing non-empty 'ruc'"
            assert isinstance(tenant.get("razonSocial"), str), "Tenant missing 'razonSocial'"
            assert isinstance(tenant.get("isActive"), bool), "'isActive' missing or not bool"
            # 'plan' can be object or null, but must exist
            assert "plan" in tenant, "'plan' key missing in tenant"
            # 'usersCount' and 'documentsCount' as numbers (int)
            assert isinstance(tenant.get("usersCount"), int), "'usersCount' missing or not int"
            assert isinstance(tenant.get("documentsCount"), int), "'documentsCount' missing or not int"
    except Exception as e:
        raise AssertionError(f"Listing tenants pagination failed: {e}")

    # Step 3: GET /v1/backoffice/tenants?search=20100070970 to verify filtering by RUC
    params_search = {
        "search": "20100070970"
    }
    try:
        resp_search = requests.get(BACKOFFICE_TENANTS_URL, headers=headers, params=params_search, timeout=30)
        assert resp_search.status_code == 200, f"Tenants search failed with status {resp_search.status_code}"
        data_search = resp_search.json()
        tenants_search = data_search.get("data")
        assert isinstance(tenants_search, list), "'data' in search response is not a list"
        # All returned tenants should have RUC containing exactly "20100070970"
        # If empty list, that is acceptable
        for tenant in tenants_search:
            ruc = tenant.get("ruc")
            assert isinstance(ruc, str), "Tenant 'ruc' missing or not string in search results"
            assert "20100070970" in ruc, f"Tenant RUC {ruc} does not match search filter '20100070970'"
    except Exception as e:
        raise AssertionError(f"Searching tenants by RUC failed: {e}")

test_backoffice_list_tenants_with_search()