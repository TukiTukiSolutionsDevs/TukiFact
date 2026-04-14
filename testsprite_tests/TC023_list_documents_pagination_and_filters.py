import requests

def test_list_documents_pagination_and_filters():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    documents_url = f"{base_url}/v1/documents"
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    timeout = 30

    # Authenticate and get token
    try:
        login_resp = requests.post(login_url, json=login_payload, timeout=timeout)
        assert login_resp.status_code == 200, f"Login failed with status code {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"
    except Exception as e:
        raise AssertionError(f"Authentication failed: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Helper to fetch documents with params and validate
    def get_documents(params):
        try:
            r = requests.get(documents_url, headers=headers, params=params, timeout=timeout)
            r.raise_for_status()
            return r.json()
        except Exception as e:
            raise AssertionError(f"GET documents failed with params {params}: {e}")

    # 1) GET /v1/documents?page=1&pageSize=5&documentType=01
    params_01 = {"page": "1", "pageSize": "5", "documentType": "01"}
    result_01 = get_documents(params_01)
    assert "data" in result_01 and isinstance(result_01["data"], list), "'data' missing or not a list"
    assert len(result_01["data"]) <= 5, "More than 5 items returned"
    for doc in result_01["data"]:
        assert doc.get("documentType") == "01", f"Document documentType is not '01': {doc.get('documentType')}"
    pagination_01 = result_01.get("pagination")
    assert pagination_01 is not None, "'pagination' missing in response"
    assert pagination_01.get("page") == 1, f"Pagination page is not 1: {pagination_01.get('page')}"
    assert pagination_01.get("pageSize") == 5, f"Pagination pageSize is not 5: {pagination_01.get('pageSize')}"
    assert isinstance(pagination_01.get("totalCount"), int) and pagination_01.get("totalCount") >= 1, "totalCount < 1"
    assert isinstance(pagination_01.get("totalPages"), int) and pagination_01.get("totalPages") >= 1, "totalPages < 1"

    # 2) GET /v1/documents?page=1&pageSize=5&documentType=03
    params_03 = {"page": "1", "pageSize": "5", "documentType": "03"}
    result_03 = get_documents(params_03)
    assert "data" in result_03 and isinstance(result_03["data"], list), "'data' missing or not a list"
    for doc in result_03["data"]:
        assert doc.get("documentType") == "03", f"Document documentType is not '03': {doc.get('documentType')}"

    # 3) GET /v1/documents?page=999&pageSize=5
    params_page999 = {"page": "999", "pageSize": "5"}
    result_page999 = get_documents(params_page999)
    assert "data" in result_page999 and isinstance(result_page999["data"], list), "'data' missing or not a list"
    assert len(result_page999["data"]) == 0, "Data is not empty on page 999"
    pagination_999 = result_page999.get("pagination")
    assert pagination_999 is not None, "'pagination' missing"
    assert isinstance(pagination_999.get("totalPages"), int) and pagination_999.get("totalPages") > 0, "totalPages not > 0"

    # 4) GET /v1/documents?page=1&pageSize=2&status=Accepted
    params_status_accepted = {"page": "1", "pageSize": "2", "status": "Accepted"}
    result_status_accepted = get_documents(params_status_accepted)
    assert "data" in result_status_accepted and isinstance(result_status_accepted["data"], list), "'data' missing or not a list"
    for doc in result_status_accepted["data"]:
        assert doc.get("status") == "Accepted", f"Document status is not 'Accepted': {doc.get('status')}"

test_list_documents_pagination_and_filters()