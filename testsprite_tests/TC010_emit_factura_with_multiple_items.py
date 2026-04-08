import requests
import uuid
import time

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENTS_URL = f"{BASE_URL}/v1/documents"

# Credentials and tenantId as provided
LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}

# Global variable to save document ID for subsequent tests (TC013, TC014, TC015, TC016)
saved_document_id = None


def test_emit_factura_with_multiple_items():
    global saved_document_id

    # Step 1: Login and obtain Bearer token
    try:
        login_resp = requests.post(
            LOGIN_URL,
            json=LOGIN_PAYLOAD,
            timeout=30
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and isinstance(access_token, str) and access_token.strip() != "", "accessToken missing or empty"
    except Exception as e:
        raise AssertionError(f"Login request failed: {str(e)}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # To avoid conflicts, generate unique serie with timestamp suffix
    unique_suffix = str(int(time.time()))
    serie_unique = f"F001"  # Series should remain F001 per instructions, but we will add unique description to avoid item conflict
    customer_name_unique = f"Cliente Test SAC {unique_suffix}"
    # Prepare payload with multiple items as per instruction
    payload = {
        "documentType": "01",
        "serie": serie_unique,
        "currency": "PEN",
        "customerDocType": "6",
        "customerDocNumber": "20100070970",
        "customerName": customer_name_unique,
        "items": [
            {
                "description": f"Servicio A {unique_suffix}",
                "quantity": 2,
                "unitPrice": 50.00,
                "unitMeasure": "ZZ",
                "igvType": "10"
            },
            {
                "description": f"Producto B {unique_suffix}",
                "quantity": 1,
                "unitPrice": 100.00,
                "unitMeasure": "NIU",
                "igvType": "10"
            }
        ]
    }

    try:
        resp = requests.post(DOCUMENTS_URL, json=payload, headers=headers, timeout=30)
    except Exception as e:
        raise AssertionError(f"POST /v1/documents request failed: {str(e)}")

    assert resp.status_code == 201, f"Expected 201 Created, got {resp.status_code}"
    try:
        resp_json = resp.json()
    except Exception:
        raise AssertionError("Response is not valid JSON")

    # Validate presence and types of required fields
    doc_id = resp_json.get("id")
    assert doc_id and isinstance(doc_id, str), "Missing or invalid 'id' in response"

    full_number = resp_json.get("fullNumber")
    assert full_number and isinstance(full_number, str), "Missing or invalid 'fullNumber' in response"
    assert full_number.startswith(f"{serie_unique}-"), f"'fullNumber' does not start with '{serie_unique}-'"

    total = resp_json.get("total")
    assert total is not None, "Missing 'total' field in response"
    # Validate total equals 236.0 (2*50 + 1*100 = 200 + 36 IGV)
    # IGV 18% on taxable base (sum of (quantity * unitPrice))
    # 2*50=100 + 1*100=100 total base=200 *1.18=236.0
    assert abs(total - 236.0) < 0.01, f"Total expected 236.0, got {total}"

    status = resp_json.get("status")
    assert status is not None, "Missing 'status' field in response"

    items = resp_json.get("items")
    assert isinstance(items, list), "'items' is not a list"
    assert len(items) == 2, f"Expected 2 items, got {len(items)}"

    # Save document ID globally for use in other tests TC013, TC014, TC015, TC016
    saved_document_id = doc_id


test_emit_factura_with_multiple_items()