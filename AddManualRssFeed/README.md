```
curl -X POST \
  'https://<your-function-app>.azurewebsites.net/api/AddManualRssFeed?code=<your-code>' \
  -H 'Cache-Control: no-cache' \
  -H 'Content-Type: application/json' \
  -d '{"date": "yyyy-MM-dd", "link": "<your-url>", "title": "<your-title>"}'
```
