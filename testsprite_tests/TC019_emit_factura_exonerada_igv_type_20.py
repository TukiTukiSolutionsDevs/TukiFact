import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENTS_URL = f"{BASE_URL}/v1/documents"


def test_emit_factura_exonerada_igv_type_20():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        # Authenticate tenant user to get access token
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status: {login_resp.status_code} {login_resp.text}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        document_payload = {
            "documentType": "01",
            "serie": "F001",
            "currency": "PEN",
            "customerDocType": "6",
            "customerDocNumber": "20512345678",
            "customerName": "Exonerado SAC",
            "items": [
                {
                    "description": "Servicio exonerado de IGV",
                    "quantity": 1,
                    "unitPrice": 1000.00,
                    "unitMeasure": "ZZ",
                    "igvType": "20"
                }
            ]
        }

        # Emit document
        emit_resp = requests.post(DOCUMENTS_URL, json=document_payload, headers=headers, timeout=30)
        assert emit_resp.status_code == 201, f"Document emission failed: {emit_resp.status_code} {emit_resp.text}"
        doc = emit_resp.json()

        # Validate response fields
        assert doc.get("documentType") == "01", "documentType mismatch"
        assert doc.get("serie") == "F001" or doc.get("fullNumber", "").startswith("F001-"), "Serie or fullNumber incorrect"
        assert "id" in doc and isinstance(doc["id"], str) and doc["id"], "Missing or invalid document id"

        # Validate IGV and total amounts
        igv = doc.get("igv")
        subtotal = doc.get("subtotal")
        total = doc.get("total")

        assert igv is not None, "Missing igv field"
        assert subtotal is not None, "Missing subtotal field"
        assert total is not None, "Missing total field"

        assert isinstance(igv, (int, float)) and igv == 0, f"Expected igv=0 but got {igv}"
        assert isinstance(subtotal, (int, float)) and subtotal == 1000.00, f"Expected subtotal=1000.00 but got {subtotal}"
        assert isinstance(total, (int, float)) and total == subtotal, f"Expected total=subtotal but got total={total} vs subtotal={subtotal}"
    except (requests.RequestException, AssertionError) as e:
        raise e


test_emit_factura_exonerada_igv_type_20()