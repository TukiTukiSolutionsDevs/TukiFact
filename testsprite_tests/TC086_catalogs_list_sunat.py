import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
CATALOGS_ENDPOINT = "/v1/catalogs"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30


def test_catalogs_list_sunat():
    # Step 1: Login to get access token
    try:
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=LOGIN_PAYLOAD,
            timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed: {login_resp.status_code} - {login_resp.text}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid or missing accessToken in login response"
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Login request or validation failed: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Accept": "application/json"
    }

    # Step 2: Get catalogs
    try:
        resp = requests.get(
            BASE_URL + CATALOGS_ENDPOINT,
            headers=headers,
            timeout=TIMEOUT
        )
        assert resp.status_code == 200, f"GET /v1/catalogs failed: {resp.status_code} - {resp.text}"
        data = resp.json()

        # Validate response not empty
        assert data, "Catalog response is empty"

        # The response may be an object with catalog keys or an array
        # Define a helper function to check if a value is a non-empty list or dict with entries
        def has_meaningful_data(value):
            if isinstance(value, list):
                return len(value) > 0
            if isinstance(value, dict):
                return len(value.keys()) > 0
            return False

        # Check at least one catalog contains SUNAT-related data such as document types or IGV types
        found_sunat_catalog = False

        if isinstance(data, dict):
            # Check each key's value for meaningful data
            for key, val in data.items():
                if has_meaningful_data(val):
                    # Check keys or values for common SUNAT catalogs keywords (loosely)
                    # We do not assume field names, so check if keys and entries suggest catalog data
                    # Examples to check inside dict or list elements
                    if isinstance(val, list):
                        for item in val:
                            if isinstance(item, dict):
                                # Check if item has keys that are typical like 'code', 'description', 'name'
                                keys_lower = [k.lower() for k in item.keys()]
                                if any(sub in key.lower() for sub in ["document", "tipo", "igv", "sunat", "catalog"]):
                                    found_sunat_catalog = True
                                    break
                                if ("code" in keys_lower and "description" in keys_lower) or ("name" in keys_lower):
                                    found_sunat_catalog = True
                                    break
                        if found_sunat_catalog:
                            break
                    elif isinstance(val, dict):
                        keys_lower = [k.lower() for k in val.keys()]
                        if any(sub in key.lower() for sub in ["document", "tipo", "igv", "sunat", "catalog"]):
                            found_sunat_catalog = True
                            break
                        if ("code" in keys_lower and "description" in keys_lower) or ("name" in keys_lower):
                            found_sunat_catalog = True
                            break
        elif isinstance(data, list):
            # If the catalog response is an array, check if it contains meaningful entries
            for entry in data:
                if has_meaningful_data(entry):
                    if isinstance(entry, dict):
                        keys_lower = [k.lower() for k in entry.keys()]
                        if ("code" in keys_lower and "description" in keys_lower) or ("name" in keys_lower):
                            found_sunat_catalog = True
                            break
        else:
            raise AssertionError("Catalog response JSON is neither object nor array")

        assert found_sunat_catalog, "No SUNAT catalog data found with meaningful entries"

    except (requests.RequestException, AssertionError, ValueError) as e:
        raise AssertionError(f"GET /v1/catalogs request or validation failed: {e}")


test_catalogs_list_sunat()