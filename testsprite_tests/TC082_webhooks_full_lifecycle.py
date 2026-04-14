import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
WEBHOOKS_URL = f"{BASE_URL}/v1/webhooks"

LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

WEBHOOK_CREATE_PAYLOAD = {
    "url": "https://httpbin.org/post",
    "events": ["document.created", "document.accepted"],
    "maxRetries": 3
}

TIMEOUT = 30


def test_webhooks_full_lifecycle():
    access_token = None
    webhook_id = None

    try:
        # Step 1: Login to get access token
        resp_login = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=TIMEOUT)
        assert resp_login.status_code == 200, f"Login failed with status {resp_login.status_code}"
        data_login = resp_login.json()
        access_token = data_login.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"

        headers = {"Authorization": f"Bearer {access_token}"}

        # Step 2: POST /v1/webhooks to create a new webhook
        resp_create = requests.post(WEBHOOKS_URL, json=WEBHOOK_CREATE_PAYLOAD, headers=headers, timeout=TIMEOUT)
        assert resp_create.status_code == 201, f"Webhook creation failed with status {resp_create.status_code}"
        data_create = resp_create.json()
        webhook_id = data_create.get("id")
        webhook_secret = data_create.get("secret")
        assert webhook_id, "Webhook ID missing in creation response"
        assert webhook_secret, "Webhook secret missing in creation response"

        # Step 3: GET /v1/webhooks and verify the new webhook is present
        resp_list = requests.get(WEBHOOKS_URL, headers=headers, timeout=TIMEOUT)
        assert resp_list.status_code == 200, f"Webhook list failed with status {resp_list.status_code}"
        data_list = resp_list.json()
        assert isinstance(data_list, list), "Webhook list response is not a list"
        assert any(w.get("id") == webhook_id for w in data_list), "Created webhook not found in webhook list"

        # Step 4: PUT /v1/webhooks/{id} with {"isActive": false}
        put_url = f"{WEBHOOKS_URL}/{webhook_id}"
        resp_put = requests.put(put_url, json={"isActive": False}, headers=headers, timeout=TIMEOUT)
        assert resp_put.status_code == 204, f"Webhook update failed with status {resp_put.status_code}"

        # Step 5: GET /v1/webhooks/{id}/deliveries and expect 200 with array
        deliveries_url = f"{WEBHOOKS_URL}/{webhook_id}/deliveries"
        resp_deliveries = requests.get(deliveries_url, headers=headers, timeout=TIMEOUT)
        assert resp_deliveries.status_code == 200, f"Webhook deliveries retrieval failed with status {resp_deliveries.status_code}"
        data_deliveries = resp_deliveries.json()
        assert isinstance(data_deliveries, list), "Webhook deliveries response is not a list"

    finally:
        # Step 6: DELETE /v1/webhooks/{id}
        if webhook_id and access_token:
            headers_del = {"Authorization": f"Bearer {access_token}"}
            del_url = f"{WEBHOOKS_URL}/{webhook_id}"
            resp_del = requests.delete(del_url, headers=headers_del, timeout=TIMEOUT)
            # Expected 204, but in case of error, should check and assert
            assert resp_del.status_code == 204, f"Webhook deletion failed with status {resp_del.status_code}"


test_webhooks_full_lifecycle()