import requests

def test_quotation_list_with_filters():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    quotations_url = f"{base_url}/v1/quotations"
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    timeout = 30

    # Step 1: Authenticate and get access token
    try:
        login_resp = requests.post(login_url, json=login_payload, timeout=timeout)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid access token received"
    except Exception as e:
        raise Exception(f"Authentication error: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Step 2: GET /v1/quotations?page=1&pageSize=10 - expect 200 with 'items' array with at least one item and 'totalCount'
    try:
        params = {"page":1,"pageSize":10}
        resp = requests.get(quotations_url, headers=headers, params=params, timeout=timeout)
        assert resp.status_code == 200, f"Quotation list request failed with status {resp.status_code}"
        resp_json = resp.json()
        items = resp_json.get("items")
        total_count = resp_json.get("totalCount")
        assert isinstance(items, list), "'items' is not a list"
        assert len(items) >= 1, "Expected at least 1 quotation item"
        assert isinstance(total_count, int) and total_count >= 1, "Invalid or missing 'totalCount'"
    except Exception as e:
        raise Exception(f"Failed retrieving quotations list: {e}")

    # Step 3: GET /v1/quotations?status=draft and verify all items have status 'draft'
    try:
        params = {"status":"draft"}
        resp = requests.get(quotations_url, headers=headers, params=params, timeout=timeout)
        assert resp.status_code == 200, f"Quotation list with filter failed with status {resp.status_code}"
        resp_json = resp.json()
        items = resp_json.get("items")
        assert isinstance(items, list), "'items' is not a list for status=draft filter"
        for item in items:
            status = item.get("status")
            assert status == "draft", f"Quotation item status expected 'draft' but got '{status}'"
    except Exception as e:
        raise Exception(f"Failed retrieving filtered quotations: {e}")

test_quotation_list_with_filters()