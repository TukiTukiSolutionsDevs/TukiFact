import requests
import sys

BASE_URL = "http://localhost:80"
BACKOFFICE_AUTH_ENDPOINT = "/v1/backoffice/auth/login"
BACKOFFICE_DOCUMENTS_ENDPOINT = "/v1/backoffice/documents"

BACKOFFICE_EMAIL = "superadmin@tukifact.net.pe"
BACKOFFICE_PASSWORD = "SuperAdmin2026!"
TIMEOUT = 30


def test_backoffice_search_documents_cross_tenant():
    session = requests.Session()

    # Authenticate to get BACKOFFICE_TOKEN
    auth_payload = {
        "email": BACKOFFICE_EMAIL,
        "password": BACKOFFICE_PASSWORD
    }
    try:
        auth_response = session.post(
            f"{BASE_URL}{BACKOFFICE_AUTH_ENDPOINT}",
            json=auth_payload,
            timeout=TIMEOUT
        )
        auth_response.raise_for_status()
    except requests.RequestException as e:
        print(f"Authentication request failed: {e}", file=sys.stderr)
        raise

    auth_json = auth_response.json()
    access_token = auth_json.get("accessToken")
    user = auth_json.get("user")

    assert access_token is not None and access_token.startswith("eyJ"), "Invalid or missing accessToken"
    assert user is not None and user.get("role") == "superadmin", "User role is not superadmin"

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # First GET documents filtered by ruc=20100070970, page=1, pageSize=5
    params_ruc = {
        "ruc": "20100070970",
        "page": 1,
        "pageSize": 5
    }
    try:
        response_ruc = session.get(
            f"{BASE_URL}{BACKOFFICE_DOCUMENTS_ENDPOINT}",
            headers=headers,
            params=params_ruc,
            timeout=TIMEOUT
        )
        response_ruc.raise_for_status()
    except requests.RequestException as e:
        print(f"GET documents filtered by ruc failed: {e}", file=sys.stderr)
        raise

    json_ruc = response_ruc.json()
    data_ruc = json_ruc.get("data")
    pagination_ruc = json_ruc.get("pagination")

    assert isinstance(data_ruc, list), "'data' is not an array"
    assert isinstance(pagination_ruc, dict), "'pagination' is not present or not an object"

    for doc in data_ruc:
        assert "tenantId" in doc and isinstance(doc["tenantId"], str) and doc["tenantId"], "Missing or invalid tenantId"
        assert "documentType" in doc and isinstance(doc["documentType"], str) and doc["documentType"], "Missing or invalid documentType"
        assert "fullNumber" in doc and isinstance(doc["fullNumber"], str) and doc["fullNumber"], "Missing or invalid fullNumber"
        assert "total" in doc and (isinstance(doc["total"], (int, float)) or isinstance(doc["total"], str)), "Missing or invalid total"
        assert "status" in doc and isinstance(doc["status"], str) and doc["status"], "Missing or invalid status"

    # Then GET documents filtered by serie=F001
    params_serie = {"serie": "F001"}
    try:
        response_serie = session.get(
            f"{BASE_URL}{BACKOFFICE_DOCUMENTS_ENDPOINT}",
            headers=headers,
            params=params_serie,
            timeout=TIMEOUT
        )
        response_serie.raise_for_status()
    except requests.RequestException as e:
        print(f"GET documents filtered by serie failed: {e}", file=sys.stderr)
        raise

    json_serie = response_serie.json()
    data_serie = json_serie.get("data")
    pagination_serie = json_serie.get("pagination")

    assert isinstance(data_serie, list), "'data' is not an array in serie filtered response"
    assert isinstance(pagination_serie, dict), "'pagination' missing or invalid in serie filtered response"

    for doc in data_serie:
        assert "tenantId" in doc and isinstance(doc["tenantId"], str) and doc["tenantId"], "Missing or invalid tenantId in serie filtered docs"
        assert "documentType" in doc and isinstance(doc["documentType"], str) and doc["documentType"], "Missing or invalid documentType in serie filtered docs"
        assert "fullNumber" in doc and isinstance(doc["fullNumber"], str) and doc["fullNumber"], "Missing or invalid fullNumber in serie filtered docs"
        assert doc["fullNumber"].startswith("F001-"), f"Document fullNumber does not start with 'F001-': {doc['fullNumber']}"
        assert "total" in doc and (isinstance(doc["total"], (int, float)) or isinstance(doc["total"], str)), "Missing or invalid total in serie filtered docs"
        assert "status" in doc and isinstance(doc["status"], str) and doc["status"], "Missing or invalid status in serie filtered docs"


test_backoffice_search_documents_cross_tenant()