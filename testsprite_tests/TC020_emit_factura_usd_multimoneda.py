import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENTS_ENDPOINT = "/v1/documents"
TIMEOUT = 30

def test_emit_factura_usd_multimoneda():
    # Step 1: Authenticate to get access token
    auth_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        auth_resp = requests.post(f"{BASE_URL}{LOGIN_ENDPOINT}", json=auth_payload, timeout=TIMEOUT)
        assert auth_resp.status_code == 200, f"Login failed with status {auth_resp.status_code}"
        auth_json = auth_resp.json()
        access_token = auth_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken in login response"
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Authentication failed: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Emit factura in USD
    document_payload = {
        "documentType": "01",
        "serie": "F001",
        "currency": "USD",
        "customerDocType": "6",
        "customerDocNumber": "20100070970",
        "customerName": "Cliente USD SAC",
        "items": [
            {
                "description": "Servicio internacional",
                "quantity": 1,
                "unitPrice": 500.00,
                "unitMeasure": "ZZ",
                "igvType": "10"
            }
        ]
    }

    document_id = None
    try:
        doc_resp = requests.post(f"{BASE_URL}{DOCUMENTS_ENDPOINT}", headers=headers, json=document_payload, timeout=TIMEOUT)
        assert doc_resp.status_code == 201, f"Document creation failed with status {doc_resp.status_code}"
        doc_json = doc_resp.json()
        # Validate currency
        currency = doc_json.get("currency")
        assert currency == "USD", f"Expected currency 'USD', got '{currency}'"
        # Validate total amount: total should be 590.00 (500 + 90 IGV)
        total = doc_json.get("total")
        assert isinstance(total, (int, float)), "Total amount missing or invalid type"
        assert abs(total - 590.00) < 0.01, f"Expected total 590.00, got {total}"
        document_id = doc_json.get("id")
        assert document_id is not None, "Document ID is missing in response"
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Emit factura USD multimoneda failed: {e}")
    finally:
        # Cleanup: Delete the created document if possible
        if document_id:
            try:
                del_resp = requests.delete(f"{BASE_URL}{DOCUMENTS_ENDPOINT}/{document_id}", headers=headers, timeout=TIMEOUT)
                # We do not assert delete success strictly, ignore errors
            except requests.RequestException:
                pass

test_emit_factura_usd_multimoneda()