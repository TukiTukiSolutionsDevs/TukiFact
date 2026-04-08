import requests

BASE_URL = "http://localhost:5186"
TIMEOUT = 30
HEADERS = {"Content-Type": "application/json"}


def test_health_ping_returns_service_info():
    url = f"{BASE_URL}/api/ping"
    try:
        response = requests.get(url, headers=HEADERS, timeout=TIMEOUT)
        response.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Request to {url} failed: {e}"

    assert response.status_code == 200, f"Expected status code 200, got {response.status_code}"
    try:
        data = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    # Validate required keys
    for key in ['service', 'version', 'environment', 'timestamp']:
        assert key in data, f"Response JSON missing key: {key}"

    # Check specific values
    assert data['service'] == 'TukiFact API', f"Expected service 'TukiFact API', got '{data['service']}'"
    assert isinstance(data['version'], str) and data['version'].strip() != "", "version must be a non-empty string"


test_health_ping_returns_service_info()