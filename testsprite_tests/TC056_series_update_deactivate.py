import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
SERIES_URL = f"{BASE_URL}/v1/series"
TIMEOUT = 30

def test_series_update_deactivate():
    # Step 0: Authenticate and get access token
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=TIMEOUT)
        assert login_resp.status_code == 200, f"Login failed: {login_resp.status_code} {login_resp.text}"
        token = login_resp.json().get("accessToken")
        assert token and token.startswith("eyJ"), "Invalid accessToken"

        headers = {
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json"
        }

        series_id = None
        created_series = False

        # Step 1: Create new series with serie 'F56'
        create_payload = {
            "documentType": "01",
            "serie": "F56",
            "emissionPoint": "0001"
        }
        create_resp = requests.post(SERIES_URL, json=create_payload, headers=headers, timeout=TIMEOUT)
        if create_resp.status_code == 201:
            resp_json = create_resp.json()
            series_id = resp_json.get("id")
            assert series_id, "Series id missing in create response"
            created_series = True
        elif create_resp.status_code == 409:
            # Conflict: series already exists, find series by GET /v1/series filtering by serie='F56'
            get_resp = requests.get(SERIES_URL, headers=headers, timeout=TIMEOUT)
            assert get_resp.status_code == 200, f"Failed to GET series list: {get_resp.status_code} {get_resp.text}"
            resp_json = get_resp.json()
            series_list = resp_json if isinstance(resp_json, list) else []
            assert isinstance(series_list, list), "Series list not found or invalid format"
            matched = [s for s in series_list if s.get("serie") == "F56"]
            assert matched, "Series F56 not found after conflict"
            series_id = matched[0].get("id")
            assert series_id, "Found series F56 but missing id"
        else:
            assert False, f"Unexpected status creating series: {create_resp.status_code} {create_resp.text}"

        # Step 2: Update the series with isActive: false
        update_url = f"{SERIES_URL}/{series_id}"
        update_payload = {
            "isActive": False
        }
        update_resp = requests.put(update_url, json=update_payload, headers=headers, timeout=TIMEOUT)
        assert update_resp.status_code == 200, f"Failed to update series: {update_resp.status_code} {update_resp.text}"

        # Step 3: Verify the series isActive is false via GET /v1/series
        verify_resp = requests.get(SERIES_URL, headers=headers, timeout=TIMEOUT)
        assert verify_resp.status_code == 200, f"Failed to list series: {verify_resp.status_code} {verify_resp.text}"
        resp_json = verify_resp.json()
        series_array = resp_json if isinstance(resp_json, list) else []
        assert isinstance(series_array, list), "Series list not found or invalid format"
        found_series = [s for s in series_array if s.get("id") == series_id]
        assert found_series, f"Series with id {series_id} not found in list"
        is_active = found_series[0].get("isActive")
        assert is_active is False, f"Series 'isActive' expected False but was {is_active}"
    finally:
        # Cleanup: delete created series if created in this test
        if 'headers' in locals() and created_series and series_id:
            del_resp = requests.delete(f"{SERIES_URL}/{series_id}", headers=headers, timeout=TIMEOUT)
            # Deletion may return 204 or 200 if successful; if 400 or other, ignore but assert no 500 or 404
            if del_resp.status_code not in (200, 204):
                raise AssertionError(f"Failed to delete series {series_id} on cleanup: {del_resp.status_code} {del_resp.text}")

test_series_update_deactivate()
