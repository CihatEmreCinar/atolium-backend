#!/usr/bin/env python3
import requests
import json

BASE_URL = "http://localhost:5178/api/v1"

# Test with the original failing user account
email = "emre@employer.com"
password = "Test@123"

print(f"Testing with account: {email}")
print("=" * 60)

# Step 1: Login with the original user
print("\n1. Logging in...")
login_resp = requests.post(f"{BASE_URL}/auth/login", json={
    "email": email,
    "password": password
})
print(f"Login Status: {login_resp.status_code}")
if login_resp.status_code != 200:
    print(f"Error: {login_resp.text}")
    exit(1)

login_data = login_resp.json()
access_token = login_data["accessToken"]
print(f"✓ Logged in successfully")

# Step 2: Get user's workshops
print("\n2. Getting workshops...")
headers = {"Authorization": f"Bearer {access_token}"}
workshops_resp = requests.get(f"{BASE_URL}/workshops/user", headers=headers)
print(f"Workshops Status: {workshops_resp.status_code}")
workshops_data = workshops_resp.json()
print(f"Found {len(workshops_data)} workshops")

if len(workshops_data) > 0:
    workshop = workshops_data[0]
    workshop_id = workshop["id"]
    print(f"Using workshop: {workshop['title']} (ID: {workshop_id})")
    
    # Step 3: Try to create a post
    print("\n3. Creating a post...")
    post_resp = requests.post(f"{BASE_URL}/posts", json={
        "workshopId": workshop_id,
        "caption": "Test post from previously failing account - should work now!"
    }, headers=headers)
    
    print(f"Post Status: {post_resp.status_code}")
    if post_resp.status_code == 201:
        print(f"✓ POST SUCCESSFUL! User can now create posts.")
        post_data = post_resp.json()
        print(f"Post ID: {post_data['id']}")
    else:
        print(f"✗ Post creation failed")
        print(f"Response: {post_resp.text}")
else:
    print("No workshops found - cannot test post creation")
