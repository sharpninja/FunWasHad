#!/bin/bash
# Script to check the status of the staging workflow monitoring system

set -e

REPO="sharpninja/FunWasHad"
BRANCH="develop"

echo "========================================"
echo "Staging Workflow Monitor Status Check"
echo "========================================"
echo ""

# Check if monitoring workflow file exists on develop
echo "🔍 Checking if monitoring workflow exists on develop branch..."
if curl -s -f "https://api.github.com/repos/$REPO/contents/.github/workflows/staging-monitor.yml?ref=$BRANCH" > /dev/null 2>&1; then
    echo "✅ Monitoring workflow EXISTS on develop branch"
    WORKFLOW_EXISTS=true
else
    echo "❌ Monitoring workflow NOT FOUND on develop branch"
    echo "   The workflow needs to be merged to develop first."
    WORKFLOW_EXISTS=false
fi
echo ""

# Check if status document exists and has been updated
echo "🔍 Checking status document..."
if curl -s -f "https://api.github.com/repos/$REPO/contents/docs/STAGING-STATUS.md?ref=$BRANCH" > /dev/null 2>&1; then
    echo "✅ Status document EXISTS on develop branch"
    
    # Fetch the content
    CONTENT=$(curl -s "https://api.github.com/repos/$REPO/contents/docs/STAGING-STATUS.md?ref=$BRANCH" | jq -r '.content' | base64 -d 2>/dev/null || echo "")
    
    if echo "$CONTENT" | grep -q "Awaiting first monitoring run"; then
        echo "⏳ Status document has NOT been updated yet (still showing placeholder)"
        REPORT_READY=false
    elif echo "$CONTENT" | grep -q "Latest Run Information"; then
        echo "✅ Status document has been UPDATED with workflow run data"
        REPORT_READY=true
        
        # Extract run number if available
        RUN_NUM=$(echo "$CONTENT" | grep -oP 'Run Number.*#\K[0-9]+' | head -1 || echo "")
        if [ -n "$RUN_NUM" ]; then
            echo "   📊 Latest run: #$RUN_NUM"
        fi
    else
        echo "⚠️  Status document exists but format is unexpected"
        REPORT_READY=false
    fi
else
    echo "❌ Status document NOT FOUND on develop branch"
    REPORT_READY=false
fi
echo ""

# Check recent staging workflow runs
echo "🔍 Checking recent staging workflow runs on develop..."
RECENT_RUN=$(curl -s "https://api.github.com/repos/$REPO/actions/workflows/staging.yml/runs?branch=$BRANCH&per_page=1" | jq -r '.workflow_runs[0]' 2>/dev/null || echo "null")

if [ "$RECENT_RUN" != "null" ]; then
    RUN_ID=$(echo "$RECENT_RUN" | jq -r '.id')
    RUN_NUM=$(echo "$RECENT_RUN" | jq -r '.run_number')
    STATUS=$(echo "$RECENT_RUN" | jq -r '.status')
    CONCLUSION=$(echo "$RECENT_RUN" | jq -r '.conclusion // "in_progress"')
    CREATED=$(echo "$RECENT_RUN" | jq -r '.created_at')
    
    echo "   Latest staging run: #$RUN_NUM (ID: $RUN_ID)"
    echo "   Status: $STATUS"
    echo "   Conclusion: $CONCLUSION"
    echo "   Created: $CREATED"
else
    echo "   ⚠️  Could not fetch recent runs"
fi
echo ""

# Summary and next steps
echo "========================================"
echo "Summary"
echo "========================================"
echo ""

if [ "$WORKFLOW_EXISTS" = true ] && [ "$REPORT_READY" = true ]; then
    echo "✅ Everything is working! The report is available at:"
    echo "   https://github.com/$REPO/blob/$BRANCH/docs/STAGING-STATUS.md"
elif [ "$WORKFLOW_EXISTS" = true ] && [ "$REPORT_READY" = false ]; then
    echo "⏳ Monitoring workflow is active but waiting for next staging run"
    echo ""
    echo "Next steps:"
    echo "1. Wait for a new push to develop branch"
    echo "2. Staging workflow will run automatically"
    echo "3. Monitoring workflow will generate the report"
    echo ""
    echo "Or trigger manually by pushing a change to develop"
elif [ "$WORKFLOW_EXISTS" = false ]; then
    echo "❌ Monitoring workflow not yet active"
    echo ""
    echo "Next steps:"
    echo "1. Merge the feature branch to develop"
    echo "2. The monitoring workflow will activate"
    echo "3. Next staging run will generate the report"
    echo ""
    echo "Current feature branch: copilot/monitor-staging-action-output"
else
    echo "⚠️  Unexpected state - manual investigation needed"
fi
echo ""

# Report location
echo "📍 Report Location:"
echo "   Repository path: docs/STAGING-STATUS.md"
echo "   GitHub URL: https://github.com/$REPO/blob/$BRANCH/docs/STAGING-STATUS.md"
echo "   Raw URL: https://raw.githubusercontent.com/$REPO/$BRANCH/docs/STAGING-STATUS.md"
echo ""
echo "📚 Documentation:"
echo "   See: WHERE-IS-THE-REPORT.md"
echo "   See: docs/deployment/Staging_Workflow_Monitoring_Implementation.md"
echo ""
