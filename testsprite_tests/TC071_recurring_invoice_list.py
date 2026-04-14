import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
RECURRING_INVOICES_URL = f"{BASE_URL}/v1/recurring-invoices"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30


def test_recurring_invoice_list():
    # Step 1: Authenticate and get Bearer token from JSON response
    login_response = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=TIMEOUT)
    assert login_response.status_code == 200, f"Login failed with status {login_response.status_code}: {login_response.text}"
    login_json = login_response.json()
    assert "accessToken" in login_json, "accessToken missing in login response"
    bearer_token = login_json["accessToken"]
    headers = {"Authorization": f"Bearer {bearer_token}"}

    # Step 2: GET /v1/recurring-invoices?page=1&pageSize=10 with Bearer token
    params = {"page": 1, "pageSize": 10}
    list_response = requests.get(RECURRING_INVOICES_URL, headers=headers, params=params, timeout=TIMEOUT)
    assert list_response.status_code == 200, f"Recurring invoices list request failed with status {list_response.status_code}: {list_response.text}"

    json_data = list_response.json()
    # Validate that 'items' is an array with at least 1 element
    assert "items" in json_data, "'items' field missing in response"
    assert isinstance(json_data["items"], list), "'items' field is not a list"
    assert len(json_data["items"]) >= 1, "'items' array is empty"

    # Validate that 'totalCount' field exists and is integer >= 1
    assert "totalCount" in json_data, "'totalCount' field missing in response"
    assert isinstance(json_data["totalCount"], int), "'totalCount' is not an integer"
    assert json_data["totalCount"] >= 1, "'totalCount' should be at least 1"


test_recurring_invoice_list()
