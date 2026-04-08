import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
SERIES_URL = f"{BASE_URL}/v1/series"
TIMEOUT = 30


def test_series_list_create_deactivate():
    # Authenticate and get token
    login_payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=TIMEOUT)
        login_resp.raise_for_status()
    except Exception as e:
        assert False, f"Login request failed: {e}"

    token = login_resp.json().get("accessToken")
    assert token and isinstance(token, str), "No accessToken received"

    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }

    # GET /v1/series
    try:
        get_resp = requests.get(SERIES_URL, headers=headers, timeout=TIMEOUT)
        get_resp.raise_for_status()
    except Exception as e:
        assert False, f"GET /v1/series failed: {e}"

    assert get_resp.status_code == 200, f"GET /v1/series returned {get_resp.status_code}, expected 200"
    series_list = get_resp.json()
    assert isinstance(series_list, list), "Response is not a JSON array"

    # POST /v1/series to create new serie "FZ99"
    create_payload = {
        "documentType": "01",
        "serie": "FZ99",
        "emissionPoint": "0001"
    }
    series_id = None
    try:
        post_resp = requests.post(SERIES_URL, headers=headers, json=create_payload, timeout=TIMEOUT)
        post_resp.raise_for_status()
    except requests.exceptions.HTTPError as he:
        assert False, f"POST /v1/series failed: {he}"
    except Exception as e:
        assert False, f"POST /v1/series request error: {e}"

    assert post_resp.status_code == 201, f"POST /v1/series returned {post_resp.status_code}, expected 201"
    post_json = post_resp.json()
    assert "id" in post_json, "Response JSON missing 'id'"
    assert post_json.get("serie") == "FZ99", f"Serie field expected 'FZ99', got {post_json.get('serie')}"
    series_id = post_json["id"]

    try:
        # PUT /v1/series/{id} to deactivate serie (isActive:false)
        put_payload = {"isActive": False}
        put_url = f"{SERIES_URL}/{series_id}"
        put_resp = requests.put(put_url, headers=headers, json=put_payload, timeout=TIMEOUT)
        # For 204 No Content, response body is empty so no json()
        assert put_resp.status_code == 204, f"PUT /v1/series/{series_id} returned {put_resp.status_code}, expected 204"

        # POST /v1/series to create duplicate serie "F001" expecting 409 Conflict
        dup_payload = {
            "documentType": "01",
            "serie": "F001",
            "emissionPoint": "0001"
        }
        dup_resp = requests.post(SERIES_URL, headers=headers, json=dup_payload, timeout=TIMEOUT)
        assert dup_resp.status_code == 409, f"POST duplicate serie returned {dup_resp.status_code}, expected 409"
    finally:
        # Cleanup: delete created serie to keep environment clean
        if series_id:
            try:
                del_url = f"{SERIES_URL}/{series_id}"
                del_resp = requests.delete(del_url, headers=headers, timeout=TIMEOUT)
                # Accept 204 No Content or 200 OK for deletion
                assert del_resp.status_code in (200, 204), f"DELETE /v1/series/{series_id} returned {del_resp.status_code}, expected 200 or 204"
            except Exception:
                pass


test_series_list_create_deactivate()