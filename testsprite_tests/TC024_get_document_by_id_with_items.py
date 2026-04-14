import requests

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENT_ENDPOINT = "/v1/documents"

# Credentials and document ID from TC017
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

DOCUMENT_ID_FROM_TC017 = "PUT_THE_DOCUMENT_ID_FROM_TC017_HERE"  # Replace with actual document ID saved from TC017

def test_get_document_by_id_with_items():
    # Step 1: Authenticate and get access token
    try:
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=LOGIN_PAYLOAD,
            timeout=30
        )
    except Exception as e:
        assert False, f"Login request failed: {e}"
    assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}: {login_resp.text}"

    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token and access_token.startswith("eyJ"), "Invalid or missing accessToken"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Accept": "application/json"
    }

    # Step 2: GET /v1/documents/{id}
    try:
        doc_resp = requests.get(
            f"{BASE_URL}{DOCUMENT_ENDPOINT}/{DOCUMENT_ID_FROM_TC017}",
            headers=headers,
            timeout=30
        )
    except Exception as e:
        assert False, f"Document GET request failed: {e}"

    assert doc_resp.status_code == 200, f"Expected 200 OK, got {doc_resp.status_code}. Response: {doc_resp.text}"

    doc_json = doc_resp.json()

    # Validate 'id' matches requested ID
    assert doc_json.get("id") == DOCUMENT_ID_FROM_TC017, f"Document id mismatch. Expected {DOCUMENT_ID_FROM_TC017}, got {doc_json.get('id')}"

    # Validate 'fullNumber' starts with 'F001-'
    full_number = doc_json.get("fullNumber")
    assert isinstance(full_number, str) and full_number.startswith("F001-"), f"fullNumber invalid or does not start with 'F001-': {full_number}"

    # Validate 'documentType' equals '01'
    assert doc_json.get("documentType") == "01", f"documentType expected '01', got {doc_json.get('documentType')}"

    # Validate 'customerName' equals 'Cliente Factura SAC'
    assert doc_json.get("customerName") == "Cliente Factura SAC", f"customerName expected 'Cliente Factura SAC', got {doc_json.get('customerName')}"

    # Validate 'items' is an array with exactly 3 elements
    items = doc_json.get("items")
    assert isinstance(items, list), f"items is not a list: {items}"
    assert len(items) == 3, f"Expected exactly 3 items, got {len(items)}"

    # Each item should have the required fields and subtotal, igv, total positive numbers
    for idx, item in enumerate(items, start=1):
        for field in ["description", "quantity", "unitPrice", "igvAmount", "subtotal", "total"]:
            assert field in item, f"Item {idx} missing field '{field}'"
        # Verify types and positive values
        try:
            subtotal = float(item["subtotal"])
            igv_amount = float(item["igvAmount"])
            total = float(item["total"])
        except (TypeError, ValueError):
            assert False, f"Item {idx} subtotal, igvAmount, or total is not convertible to float"
        assert subtotal > 0, f"Item {idx} subtotal not positive: {subtotal}"
        assert igv_amount >= 0, f"Item {idx} igvAmount negative: {igv_amount}"
        assert total > 0, f"Item {idx} total not positive: {total}"

    # Verify document level subtotal, igv, total are positive numbers (if present)
    for field in ["subtotal", "igv", "total"]:
        if field in doc_json:
            try:
                val = float(doc_json[field])
            except (TypeError, ValueError):
                assert False, f"Document field '{field}' not convertible to float"
            assert val > 0, f"Document field '{field}' is not positive: {val}"

    print("Test TC024 get_document_by_id_with_items passed.")

test_get_document_by_id_with_items()