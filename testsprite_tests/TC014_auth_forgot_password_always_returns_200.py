import requests

def test_auth_forgot_password_always_returns_200():
    base_url = "http://localhost:80"
    endpoint = "/v1/auth/forgot-password"
    url = base_url + endpoint
    headers = {"Content-Type": "application/json"}
    timeout = 30

    emails_to_test = [
        "prdtest@test.pe",
        "nonexistent@fake.pe"
    ]

    for email in emails_to_test:
        payload = {"email": email}
        try:
            response = requests.post(url, json=payload, headers=headers, timeout=timeout)
        except requests.RequestException as e:
            assert False, f"Request failed for email '{email}': {e}"
        assert response.status_code == 200, f"Expected 200 OK for email '{email}', got {response.status_code}"
        try:
            json_data = response.json()
        except ValueError:
            assert False, f"Response is not valid JSON for email '{email}'"
        assert "message" in json_data, f"'message' field missing in response JSON for email '{email}'"

test_auth_forgot_password_always_returns_200()