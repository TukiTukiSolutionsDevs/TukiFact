import requests

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENT_XML_ENDPOINT_TEMPLATE = "/v1/documents/{id}/xml"

# The document ID from TC017 as per provided info
DOCUMENT_ID_TC017 = None

def get_access_token():
    login_url = BASE_URL + LOGIN_ENDPOINT
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        response = requests.post(login_url, json=login_payload, timeout=30)
        response.raise_for_status()
        json_resp = response.json()
        token = json_resp.get("accessToken")
        assert token and token.startswith("eyJ"), "Invalid accessToken received"
        return token
    except Exception as e:
        raise AssertionError(f"Authentication failed: {e}")

def test_download_document_xml():
    global DOCUMENT_ID_TC017

    access_token = get_access_token()
    headers = {"Authorization": f"Bearer {access_token}"}

    if DOCUMENT_ID_TC017 is None:
        create_doc_url = BASE_URL + "/v1/documents"
        payload = {
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
        doc_id = None
        try:
            resp = requests.post(create_doc_url, json=payload, headers=headers, timeout=30)
            resp.raise_for_status()
            doc_json = resp.json()
            doc_id = doc_json.get("id")
            assert doc_id is not None and len(doc_id) > 0, "Document ID not returned"
            DOCUMENT_ID_TC017 = doc_id

            xml_url = BASE_URL + DOCUMENT_XML_ENDPOINT_TEMPLATE.format(id=doc_id)
            xml_resp = requests.get(xml_url, headers=headers, timeout=30)
            assert xml_resp.status_code == 200, f"Expected 200 OK, got {xml_resp.status_code}"
            content_type = xml_resp.headers.get("Content-Type", "")
            assert "application/xml" in content_type, f"Content-Type does not contain 'application/xml': {content_type}"
            content = xml_resp.text
            assert content.strip().startswith("<?xml") or ("<xml" in content.lower()), "Response body does not contain XML content"
        finally:
            pass
    else:
        xml_url = BASE_URL + DOCUMENT_XML_ENDPOINT_TEMPLATE.format(id=DOCUMENT_ID_TC017)
        try:
            xml_resp = requests.get(xml_url, headers=headers, timeout=30)
            assert xml_resp.status_code == 200, f"Expected 200 OK, got {xml_resp.status_code}"
            content_type = xml_resp.headers.get("Content-Type", "")
            assert "application/xml" in content_type, f"Content-Type does not contain 'application/xml': {content_type}"
            content = xml_resp.text
            assert content.strip().startswith("<?xml") or ("<xml" in content.lower()), "Response body does not contain XML content"
        except Exception as e:
            raise AssertionError(f"Failed to get XML for document {DOCUMENT_ID_TC017}: {e}")

test_download_document_xml()
