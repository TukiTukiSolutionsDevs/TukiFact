import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENT_PDF_URL_TEMPLATE = f"{BASE_URL}/v1/documents/{{id}}/pdf"
DOCUMENT_ID_TC017 = "id-from-TC017"  # Replace this with the actual document ID saved from TC017


def test_download_document_pdf():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    try:
        # Authenticate to get access token
        login_response = requests.post(LOGIN_URL, json=login_payload, timeout=30)
        assert login_response.status_code == 200, f"Login failed with status {login_response.status_code}"
        login_data = login_response.json()
        access_token = login_data.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid access token received"

        headers = {"Authorization": f"Bearer {access_token}"}

        # Use the known document ID from TC017
        document_id = DOCUMENT_ID_TC017
        assert document_id and len(document_id) > 0, "Document ID from TC017 must be provided"

        pdf_url = DOCUMENT_PDF_URL_TEMPLATE.format(id=document_id)
        pdf_response = requests.get(pdf_url, headers=headers, timeout=30)
        # 200 OK expected
        assert pdf_response.status_code == 200, f"Failed to download PDF, status: {pdf_response.status_code}"
        content_type = pdf_response.headers.get("Content-Type", "")
        assert "application/pdf" in content_type.lower(), f"Unexpected Content-Type: {content_type}"
        content = pdf_response.content
        assert content and len(content) > 0, "PDF content is empty"
        # Check PDF magic number (%PDF)
        assert content[:4] == b"%PDF", "Response content does not start with PDF header '%PDF'"

    except requests.RequestException as e:
        assert False, f"Request failed: {e}"


test_download_document_pdf()
