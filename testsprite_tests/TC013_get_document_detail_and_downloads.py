import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENT_URL = f"{BASE_URL}/v1/documents"

LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}

def test_get_document_detail_and_downloads():
    # Step 1: Login to get access token
    try:
        login_resp = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=30)
        login_resp.raise_for_status()
    except Exception as e:
        assert False, f"Login request failed: {e}"

    login_data = login_resp.json()
    access_token = login_data.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.strip(), "Login succeeded but no valid accessToken found"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Get the document ID saved from TC010 by listing documents and finding one that matches criteria
    # Since no direct way to get stored ID, we try to find a document that matches:
    # We expect a document with 2 items, fullNumber starting with 'F001-'
    # To be robust, retrieve first 10 documents and pick one matching criteria

    # Note: TC010 emits a document with serie "F001", 2 items, total 236.0
    # We'll do a GET /v1/documents?page=1&pageSize=10 and try to find such a document
    params = {"page": 1, "pageSize": 10}

    try:
        docs_resp = requests.get(DOCUMENT_URL, headers=headers, params=params, timeout=30)
        docs_resp.raise_for_status()
    except Exception as e:
        assert False, f"Failed to list documents to find the required document: {e}"

    docs_data = docs_resp.json()
    documents = docs_data.get("data", [])
    assert isinstance(documents, list), "Documents list missing in response"

    document_id = None
    # For each document, fetch detail and check if it matches criteria (2 items, fullNumber starts with 'F001-')
    for doc in documents:
        doc_id = doc.get("id")
        if not doc_id:
            continue
        try:
            detail_resp = requests.get(f"{DOCUMENT_URL}/{doc_id}", headers=headers, timeout=30)
            if detail_resp.status_code != 200:
                continue
            detail = detail_resp.json()
            if (
                detail.get("fullNumber", "").startswith("F001-")
                and isinstance(detail.get("items"), list)
                and len(detail["items"]) == 2
            ):
                document_id = doc_id
                break
        except:
            continue

    assert document_id, "Could not find a suitable document ID matching TC010 document criteria"

    # Step 3: GET /v1/documents/{id}
    try:
        resp_detail = requests.get(f"{DOCUMENT_URL}/{document_id}", headers=headers, timeout=30)
        resp_detail.raise_for_status()
    except Exception as e:
        assert False, f"Failed to fetch document detail: {e}"

    detail_json = resp_detail.json()
    # Validate required fields
    assert "fullNumber" in detail_json and detail_json["fullNumber"].startswith("F001-"), "fullNumber missing or invalid"
    assert "items" in detail_json and isinstance(detail_json["items"], list) and len(detail_json["items"]) == 2, "items array missing or incorrect length"
    assert "total" in detail_json and isinstance(detail_json["total"], (int, float)), "total missing or invalid"
    assert "status" in detail_json and isinstance(detail_json["status"], str), "status missing or invalid"

    # Step 4: GET /v1/documents/{id}/pdf (expect binary content)
    try:
        resp_pdf = requests.get(f"{DOCUMENT_URL}/{document_id}/pdf", headers=headers, timeout=30)
        resp_pdf.raise_for_status()
    except Exception as e:
        assert False, f"Failed to fetch PDF document: {e}"

    # Validate content type and content
    content_type_pdf = resp_pdf.headers.get("Content-Type", "")
    assert resp_pdf.content and content_type_pdf in ["application/pdf", "application/octet-stream"], f"PDF content missing or invalid content-type: {content_type_pdf}"

    # Step 5: GET /v1/documents/{id}/xml (expect content)
    try:
        resp_xml = requests.get(f"{DOCUMENT_URL}/{document_id}/xml", headers=headers, timeout=30)
        resp_xml.raise_for_status()
    except Exception as e:
        assert False, f"Failed to fetch XML document: {e}"

    # Validate content type and content presence (usually application/xml or text/xml)
    content_type_xml = resp_xml.headers.get("Content-Type", "")
    assert resp_xml.content, "XML document content is empty"
    assert content_type_xml in ["application/xml", "text/xml", "application/octet-stream"], f"XML content-type invalid: {content_type_xml}"

test_get_document_detail_and_downloads()