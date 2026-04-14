import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENTS_ENDPOINT = "/v1/documents"
TIMEOUT = 30

def test_emit_factura_01_with_multiple_items_and_igv():
    auth_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        # Authenticate and get Bearer token
        auth_resp = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=auth_payload,
            timeout=TIMEOUT
        )
        assert auth_resp.status_code == 200, f"Expected 200 OK for login, got {auth_resp.status_code}"
        auth_json = auth_resp.json()
        access_token = auth_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid or missing accessToken"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        document_payload = {
            "documentType": "01",
            "serie": "F001",
            "currency": "PEN",
            "customerDocType": "6",
            "customerDocNumber": "20100070970",
            "customerName": "Cliente Factura SAC",
            "customerAddress": "Av Test 123, Lima",
            "customerEmail": "cliente@test.pe",
            "notes": "Test factura PRD",
            "items": [
                {
                    "description": "Servicio de consultoría TI",
                    "quantity": 2,
                    "unitPrice": 500.00,
                    "unitMeasure": "ZZ",
                    "igvType": "10"
                },
                {
                    "description": "Licencia de software anual",
                    "quantity": 1,
                    "unitPrice": 1200.00,
                    "unitMeasure": "NIU",
                    "igvType": "10"
                },
                {
                    "description": "Capacitación técnica",
                    "quantity": 3,
                    "unitPrice": 150.00,
                    "unitMeasure": "ZZ",
                    "igvType": "10",
                    "discount": 50.00
                }
            ]
        }

        # Emit the factura document
        doc_resp = requests.post(
            f"{BASE_URL}{DOCUMENTS_ENDPOINT}",
            json=document_payload,
            headers=headers,
            timeout=TIMEOUT
        )
        assert doc_resp.status_code == 201, f"Expected 201 Created, got {doc_resp.status_code}"
        doc_json = doc_resp.json()

        document_id = doc_json.get("id")
        assert document_id, "Response missing 'id'"
        assert isinstance(document_id, str), "'id' should be a string (UUID)"
        full_number = doc_json.get("fullNumber")
        assert full_number and full_number.startswith("F001-"), "'fullNumber' missing or does not start with 'F001-'"
        assert doc_json.get("documentType") == "01", "'documentType' is not '01'"
        allowed_statuses = {"Accepted", "Signed", "Sent", "Created"}
        status = doc_json.get("status")
        # Normalize status string to capitalize first letter
        if isinstance(status, str):
            status = status.capitalize()
        assert status in allowed_statuses, f"'status' value '{status}' not in expected {allowed_statuses}"

        items = doc_json.get("items")
        assert isinstance(items, list) and len(items) == 3, f"'items' array must have exactly 3 elements"

        subtotal = doc_json.get("subtotal")
        igv = doc_json.get("igv")
        total = doc_json.get("total")
        # Verify subtotal, igv, total are numbers and > 0
        assert isinstance(subtotal, (int, float)) and subtotal > 0, "'subtotal' must be a number > 0"
        assert isinstance(igv, (int, float)) and igv > 0, "'igv' must be a number > 0"
        assert isinstance(total, (int, float)) and total > 0, "'total' must be a number > 0"

        # Calculate expected values
        # subtotal = 2*500 + 1*1200 + (3*150 - 50) = 2600
        expected_subtotal = 2*500 + 1*1200 + (3*150 - 50)
        expected_igv = 468
        expected_total = 3068

        # Accept small float rounding tolerances
        def almost_equal(a, b, tol=0.01):
            return abs(a - b) <= tol

        assert almost_equal(subtotal, expected_subtotal), f"Expected subtotal {expected_subtotal}, got {subtotal}"
        assert almost_equal(igv, expected_igv), f"Expected igv {expected_igv}, got {igv}"
        assert almost_equal(total, expected_total), f"Expected total {expected_total}, got {total}"

        # Save document id for further tests - storing in a global or file would happen in real test env
        global EMITTED_DOCUMENT_ID_TC017
        EMITTED_DOCUMENT_ID_TC017 = document_id

    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Test TC017 failed: {e}")

test_emit_factura_01_with_multiple_items_and_igv()
