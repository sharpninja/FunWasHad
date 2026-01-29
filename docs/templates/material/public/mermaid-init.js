// Mermaid.js initialization for DocFX
(function() {
  // Load Mermaid.js from CDN
  var script = document.createElement('script');
  script.src = 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js';
  script.onload = function() {
    mermaid.initialize({ 
      startOnLoad: true,
      theme: 'default',
      gantt: {
        axisFormat: '%Y-%m-%d',
        leftPadding: 75,
        gridLineStartPadding: 35,
        fontSize: 11,
        fontFamily: '"Roboto", sans-serif',
        numberSectionStyles: 4,
        bottomPadding: 25
      }
    });
    
    // Initialize Mermaid for any diagrams
    function initMermaid() {
      var mermaidElements = document.querySelectorAll('.language-mermaid, code.language-mermaid');
      if (mermaidElements.length > 0) {
        mermaid.init(undefined, mermaidElements);
      }
    }
    
    // Initialize on DOM ready
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', initMermaid);
    } else {
      initMermaid();
    }
  };
  document.head.appendChild(script);
})();
