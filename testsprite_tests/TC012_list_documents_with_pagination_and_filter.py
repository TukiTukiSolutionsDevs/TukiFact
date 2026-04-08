import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENTS_URL = f"{BASE_URL}/v1/documents"
LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84",
}
TIMEOUT = 30


def test_list_documents_with_pagination_and_filter():
    # Step 1: Login to get Bearer token
    try:
        login_resp = requests.post(
            LOGIN_URL, json=LOGIN_PAYLOAD, timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and isinstance(access_token, str), "Invalid accessToken"

        headers = {"Authorization": f"Bearer {access_token}", "Content-Type": "application/json"}

        # Step 2: GET /v1/documents?page=1&pageSize=5&documentType=01
        params = {"page": 1, "pageSize": 5, "documentType": "01"}
        resp = requests.get(DOCUMENTS_URL, headers=headers, params=params, timeout=TIMEOUT)
        assert resp.status_code == 200, f"Expected 200 OK, got {resp.status_code}"

        json_data = resp.json()
        assert isinstance(json_data, dict), "Response JSON is not a dictionary"
        assert "data" in json_data, "'data' field missing in response"
        assert "pagination" in json_data, "'pagination' field missing in response"

        data = json_data["data"]
        pagination = json_data["pagination"]

        assert isinstance(data, list), "'data' is not a list"
        assert len(data) <= 5, f"'data' length exceeded pageSize: {len(data)}"

        # Check all items have documentType '01'
        for item in data:
            assert isinstance(item, dict), "Item in data is not dict"
            doc_type = item.get("documentType")
            assert doc_type == "01", f"DocumentType mismatch: expected '01', got '{doc_type}'"

        # Validate pagination object fields and types
        expected_pagination_fields = ["page", "pageSize", "totalCount", "totalPages"]
        for field in expected_pagination_fields:
            assert field in pagination, f"'{field}' missing in pagination"
        assert isinstance(pagination["page"], int), "'page' is not int"
        assert pagination["page"] == 1, "'page' value incorrect"
        assert isinstance(pagination["pageSize"], int), "'pageSize' is not int"
        assert pagination["pageSize"] == 5, "'pageSize' value incorrect"
        assert isinstance(pagination["totalCount"], int), "'totalCount' is not int"
        assert isinstance(pagination["totalPages"], int), "'totalPages' is not int"

        # Step 3: GET /v1/documents?page=999&pageSize=5
        params = {"page": 999, "pageSize": 5}
        resp2 = requests.get(DOCUMENTS_URL, headers=headers, params=params, timeout=TIMEOUT)
        assert resp2.status_code == 200, f"Expected 200 OK on page 999, got {resp2.status_code}"

        json_data2 = resp2.json()
        assert "data" in json_data2, "'data' field missing in response for page 999"
        data2 = json_data2["data"]
        assert isinstance(data2, list), "'data' is not a list for page 999"
        assert len(data2) == 0, f"Expected empty data array on page 999, got {len(data2)}"

    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Test failed: {e}")


test_list_documents_with_pagination_and_filter()