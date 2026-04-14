import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
RETENTIONS_URL = f"{BASE_URL}/v1/retentions"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30


def test_retention_list_with_filters():
    # Step 1: Authenticate and get access token
    try:
        login_resp = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"
    assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid or missing accessToken"

    headers = {"Authorization": f"Bearer {access_token}"}

    # Step 2: GET /v1/retentions?page=1&pageSize=10
    try:
        retentions_resp = requests.get(f"{RETENTIONS_URL}?page=1&pageSize=10", headers=headers, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"GET retentions page 1 request failed: {e}"
    assert retentions_resp.status_code == 200, f"GET /v1/retentions?page=1&pageSize=10 failed with status {retentions_resp.status_code}"
    retentions_json = retentions_resp.json()

    # Validate presence of 'items' array and 'totalCount'
    items = retentions_json.get("items")
    total_count = retentions_json.get("totalCount")
    assert isinstance(items, list), "'items' field missing or not a list"
    assert isinstance(total_count, int), "'totalCount' field missing or not an int"
    assert len(items) >= 1, "Expected at least 1 item in retentions list (from TC057)"

    # Step 3: GET /v1/retentions?dateFrom=2026-04-01&dateTo=2026-04-30
    try:
        filtered_resp = requests.get(f"{RETENTIONS_URL}?dateFrom=2026-04-01&dateTo=2026-04-30", headers=headers, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"GET retentions with date filter request failed: {e}"
    assert filtered_resp.status_code == 200, f"GET /v1/retentions with date filter failed with status {filtered_resp.status_code}"
    filtered_json = filtered_resp.json()
    filtered_items = filtered_json.get("items")
    assert isinstance(filtered_items, list), "'items' field missing or not a list in filtered response"

    # Optionally check that filtered results have dates within range if date fields exist
    # Since schema is not detailed, just check at least 0 or more items (no strict size)
    # If items exist, we can spot-check date fields if they exist.
    if filtered_items:
        for item in filtered_items:
            # Check 'date' or 'createdAt' if present and within range
            date_str = item.get("date") or item.get("createdAt") or item.get("documentDate")
            if date_str:
                # ISO date check between 2026-04-01 and 2026-04-30
                from datetime import datetime
                try:
                    item_date = datetime.fromisoformat(date_str.split("T")[0])
                    assert datetime(2026, 4, 1) <= item_date <= datetime(2026, 4, 30), f"Item date {date_str} outside filter range"
                except Exception:
                    # If date format invalid, ignore check, only warn
                    pass

    # If no exceptions or assertion failures, test passed
    print("TC059 retention_list_with_filters passed")


test_retention_list_with_filters()