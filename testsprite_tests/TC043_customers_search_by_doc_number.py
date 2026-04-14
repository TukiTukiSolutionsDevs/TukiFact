import requests

def test_customers_search_by_doc_number():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    search_url_template = f"{base_url}/v1/customers/search"

    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    try:
        # Step 1: Login to get access token
        login_response = requests.post(login_url, json=login_payload, timeout=30)
        assert login_response.status_code == 200, f"Login failed: {login_response.text}"
        login_json = login_response.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"

        headers = {
            "Authorization": f"Bearer {access_token}"
        }

        # Step 2: GET /v1/customers/search?docNumber=20567890123
        params_found = {"docNumber": "20567890123"}
        search_response_found = requests.get(search_url_template, headers=headers, params=params_found, timeout=30)
        assert search_response_found.status_code == 200, f"Expected 200 OK for existing docNumber, got {search_response_found.status_code}"
        search_json_found = search_response_found.json()
        # Check 'docNumber' and 'name' in the JSON root object (likely)
        assert isinstance(search_json_found, dict), "Search response is not a dict"
        doc_number = search_json_found.get("docNumber")
        name = search_json_found.get("name")
        assert doc_number == "20567890123", f"Expected docNumber '20567890123', got {doc_number}"
        assert name == "Cliente Empresa SAC", f"Expected name 'Cliente Empresa SAC', got {name}"

        # Step 3: GET /v1/customers/search?docNumber=99999999999 (non-existing)
        params_not_found = {"docNumber": "99999999999"}
        search_response_not_found = requests.get(search_url_template, headers=headers, params=params_not_found, timeout=30)
        assert search_response_not_found.status_code == 404, f"Expected 404 Not Found for non-existing docNumber, got {search_response_not_found.status_code}"

    except (AssertionError, requests.RequestException) as e:
        raise AssertionError(f"Test failed: {e}")

test_customers_search_by_doc_number()