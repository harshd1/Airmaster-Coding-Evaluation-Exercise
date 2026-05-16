Add-Type -AssemblyName System.Net.Http

$client = New-Object System.Net.Http.HttpClient
try {
    $response = $client.GetAsync("http://localhost:5000/api/products").Result
    Write-Host "Backend Status: $($response.StatusCode)"
    $content = $response.Content.ReadAsStringAsync().Result
    $products = $content | ConvertFrom-Json
    Write-Host "Products returned: $($products.Count)"
    foreach ($product in $products) {
        Write-Host "  Product: $($product.Name)"
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}
