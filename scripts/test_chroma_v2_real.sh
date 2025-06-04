#!/bin/bash

echo "🔍 Testando ChromaDB API v2 REAL (Multi-tenant)..."

# 1. Heartbeat
echo ""
echo "1️⃣ Heartbeat v2:"
curl -s "http://localhost:8000/api/v2/heartbeat"

# 2. Versão
echo ""
echo "2️⃣ Versão:"
curl -s "http://localhost:8000/api/v2/version"

# 3. Identidade do usuário (tenant padrão)
echo ""
echo "3️⃣ Identidade do usuário:"
curl -s "http://localhost:8000/api/v2/auth/identity"

# 4. Criar tenant "devops"
echo ""
echo "4️⃣ Criando tenant 'devops':"
curl -X POST "http://localhost:8000/api/v2/tenants" \
     -H "Content-Type: application/json" \
     -d '{"name": "devops"}'

# 5. Verificar tenant criado
echo ""
echo "5️⃣ Verificando tenant 'devops':"
curl -s "http://localhost:8000/api/v2/tenants/devops"

# 6. Criar database "knowledge" no tenant "devops"
echo ""
echo "6️⃣ Criando database 'knowledge':"
curl -X POST "http://localhost:8000/api/v2/tenants/devops/databases" \
     -H "Content-Type: application/json" \
     -d '{"name": "knowledge"}'

# 7. Listar databases no tenant
echo ""
echo "7️⃣ Listando databases:"
curl -s "http://localhost:8000/api/v2/tenants/devops/databases"

# 8. Criar coleção "documentation"
echo ""
echo "8️⃣ Criando coleção 'documentation':"
curl -X POST "http://localhost:8000/api/v2/tenants/devops/databases/knowledge/collections" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "documentation",
       "metadata": {
         "description": "DevOps documentation collection",
         "created_by": "api_test"
       },
       "get_or_create": true
     }'

# 9. Listar coleções
echo ""
echo "9️⃣ Listando coleções:"
curl -s "http://localhost:8000/api/v2/tenants/devops/databases/knowledge/collections"

echo ""
echo "✅ Estrutura multi-tenant testada!"
echo "📖 Swagger completo: http://localhost:8000/docs"