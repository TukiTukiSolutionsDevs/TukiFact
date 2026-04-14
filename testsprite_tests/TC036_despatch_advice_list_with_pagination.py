import requests

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DESPATCH_ADVICES_ENDPOINT = "/v1/despatch-advices"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30

def test_despatch_advice_list_with_pagination():
    # Step 1: Authenticate and get access token
    try:
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=LOGIN_PAYLOAD,
            timeout=TIMEOUT
        )
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"

    assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token and access_token.startswith("eyJ"), "Invalid or missing accessToken"
    headers = {"Authorization": f"Bearer {access_token}"}

    # Step 2: GET /v1/despatch-advices?page=1&pageSize=10
    params = {"page": 1, "pageSize": 10}
    try:
        resp = requests.get(
            BASE_URL + DESPATCH_ADVICES_ENDPOINT,
            headers=headers,
            params=params,
            timeout=TIMEOUT
        )
    except requests.RequestException as e:
        assert False, f"GET despatch-advices request failed: {e}"

    assert resp.status_code == 200, f"Expected 200 OK but got {resp.status_code}"

    json_data = resp.json()
    assert isinstance(json_data, dict), "Response is not a JSON object"

    data = json_data.get("data")
    pagination = json_data.get("pagination")

    assert isinstance(data, list), "'data' field is missing or not a list"
    assert len(data) >= 1, "Expected at least 1 item in 'data' array"
    assert isinstance(pagination, dict), "'pagination' field is missing or not a dict"

    # Check pagination fields presence and types
    required_pagination_fields = ["page", "pageSize", "totalCount", "totalPages"]
    for field in required_pagination_fields:
        assert field in pagination, f"'pagination' missing field '{field}'"
        assert isinstance(pagination[field], int), f"'{field}' in 'pagination' should be int"

    # Validate pagination values
    assert pagination["page"] == 1, f"'page' should be 1 but is {pagination['page']}"
    assert pagination["pageSize"] == 10, f"'pageSize' should be 10 but is {pagination['pageSize']}"
    assert pagination["totalCount"] >= 1, "'totalCount' should be at least 1"
    assert pagination["totalPages"] >= 1, "'totalPages' should be at least 1"

test_despatch_advice_list_with_pagination()