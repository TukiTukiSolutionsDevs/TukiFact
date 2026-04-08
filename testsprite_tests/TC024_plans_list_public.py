import requests

BASE_URL = "http://localhost:5186"

def test_plans_list_public():
    url = f"{BASE_URL}/v1/plans"
    headers = {
        "Content-Type": "application/json"
    }
    try:
        response = requests.get(url, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"
    
    assert response.status_code == 200, f"Expected status code 200, got {response.status_code}"
    try:
        plans = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"
    
    assert isinstance(plans, list), f"Expected response body to be a JSON array, got {type(plans)}"
    assert len(plans) > 0, "Expected at least one plan in the list"
    for plan in plans:
        assert isinstance(plan, dict), "Each plan should be a JSON object"
        assert "name" in plan, "Each plan should have 'name' field"
        assert "features" in plan, "Each plan should have 'features' field"

test_plans_list_public()