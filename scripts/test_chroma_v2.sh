#!/bin/bash

echo "🔍 Testando ChromaDB API v2..."

# Verificar heartbeat v2
echo ""
echo "1️⃣ Heartbeat v2..."
curl -s "http://localhost:8000/api/v2/heartbeat" && echo " ✅ v2 online" || echo " ❌ v2 offline"

# Verificar versão v2
echo ""
echo "2️⃣ Versão v2..."
curl -s "http://localhost:8000/api/v2/version" | jq '.' 2>/dev/null || curl -s "http://localhost:8000/api/v2/version"

# Listar coleções v2
echo ""
echo "3️⃣ Listando coleções v2..."
curl -s "http://localhost:8000/api/v2/collections" | jq '.' 2>/dev/null || curl -s "http://localhost:8000/api/v2/collections"

# Criar coleção de teste v2
echo ""
echo "4️⃣ Criando coleção teste v2..."
curl -X POST "http://localhost:8000/api/v2/collections" \
     -H "Content-Type: application/json" \
     -d '{"name":"test-v2","metadata":{"description":"Test v2 collection"},"get_or_create":true}' \
     | jq '.' 2>/dev/null || echo "Resposta raw:"

curl -X POST "http://localhost:8000/api/v2/collections" \
     -H "Content-Type: application/json" \
     -d '{"name":"test-v2","metadata":{"description":"Test v2 collection"},"get_or_create":true}'

# Verificar coleção criada v2
echo ""
echo "5️⃣ Verificando coleção criada v2..."
curl -s "http://localhost:8000/api/v2/collections/test-v2" | jq '.' 2>/dev/null || curl -s "http://localhost:8000/api/v2/collections/test-v2"

# Adicionar documento teste v2
echo ""
echo "6️⃣ Adicionando documento teste v2..."
curl -X POST "http://localhost:8000/api/v2/collections/test-v2/add" \
     -H "Content-Type: application/json" \
     -d '{"ids":["test1"],"documents":["Docker é uma ferramenta de containerização"],"metadatas":[{"category":"test"}]}' \
     | jq '.' 2>/dev/null || echo "Resposta raw:"

curl -X POST "http://localhost:8000/api/v2/collections/test-v2/add" \
     -H "Content-Type: application/json" \
     -d '{"ids":["test1"],"documents":["Docker é uma ferramenta de containerização"],"metadatas":[{"category":"test"}]}'

# Buscar documento v2
echo ""
echo "7️⃣ Buscando documento v2..."
curl -X POST "http://localhost:8000/api/v2/collections/test-v2/query" \
     -H "Content-Type: application/json" \
     -d '{"query_texts":["Docker"],"n_results":1,"include":["documents","metadatas","distances"]}' \
     | jq '.' 2>/dev/null || echo "Resposta raw:"

curl -X POST "http://localhost:8000/api/v2/collections/test-v2/query" \
     -H "Content-Type: application/json" \
     -d '{"query_texts":["Docker"],"n_results":1,"include":["documents","metadatas","distances"]}'

echo ""
echo "✅ Teste v2 concluído!"