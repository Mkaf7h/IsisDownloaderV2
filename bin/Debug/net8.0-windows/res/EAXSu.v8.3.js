// ==UserScript==
// @name         EAX made by سكونا العتيبي - Nzf
// @namespace    eax.audio.enhancer
// @version      8.3
// @description  EAX Audio Enhancer - Full Suite
// @match        https://discord.com/*
// @match        https://*.discord.com/*
// @grant        none
// @run-at       document-start
// ==/UserScript==

(function () {
  'use strict';
  if (window.__eaxEQv83) return;
  window.__eaxEQv83 = true;

  var CORE = function () {
    if (window.__eaxCore) return;
    window.__eaxCore = true;

    var cfg = {
      monitorOn: false, monitorLevel: 0.3,
      eaxOn: true, eaxPreset: 'Haunted Cavern', eaxAmount: 0,
      // EQ
      eqOn: false, eq125: 0, eq250: 0, eq500: 0, eq1k: 0, eq2k: 0, eq4k: 0, eq8k: 0,
      // ML Boost (شريط واحد)
      boostOn: true, boostGain: 20.0, boostDrive: 0.85,
      // Pitch
      pitchOn: false, pitchSemi: 0,
      // Soundboard
      soundActive: true, soundVol: 1.0
    };

    var ctx = null, mixBus = null, outDest = null, revN = null, mic = null;
    var micMonitorGain = null;
    var soundGain = null, soundBuf = null, soundSrc = null, soundName = '';
    
    var eqNodes = {};
    var boostPre = null, boostShaper = null, boostPost = null;

    function ev(n, d) { window.dispatchEvent(new CustomEvent(n, { detail: d || {} })); }
    function log() { try { console.log.apply(console, ['[EAX]'].concat([].slice.call(arguments))); } catch(e){} }

    function ensure() {
      if (ctx) {
        if (ctx.state === 'suspended') ctx.resume().catch(function(){});
        return ctx;
      }
      try {
        var C = window.AudioContext || window.webkitAudioContext;
        if (!C) { ev('eax-status', { ctx: 'unsupported' }); return null; }
        ctx = new C();
      } catch (e) { ev('eax-status', { ctx: 'error' }); return null; }

      mixBus = ctx.createGain();
      mixBus.gain.value = 1.0;

      // 1. Equalizer (7 Bands)
      var freqs = [125, 250, 500, 1000, 2000, 4000, 8000];
      var prevNode = mixBus;
      freqs.forEach(function(f) {
        var filter = ctx.createBiquadFilter();
        filter.type = 'peaking';
        filter.frequency.value = f;
        filter.Q.value = 1.0;
        filter.gain.value = 0;
        prevNode.connect(filter);
        prevNode = filter;
        eqNodes[f] = filter;
      });

      // 2. ML Boost (شريط واحد)
      boostPre = ctx.createGain();
      boostPre.gain.value = 1.0;
      boostShaper = ctx.createWaveShaper();
      boostShaper.curve = makeBoostCurve(0);
      boostShaper.oversample = '4x';
      boostPost = ctx.createGain();
      boostPost.gain.value = 0.5;
      
      prevNode.connect(boostPre);
      boostPre.connect(boostShaper);
      boostShaper.connect(boostPost);
      prevNode = boostPost;

      // 3. EAX Reverb
      var revInput = ctx.createGain();
      var revOutput = ctx.createGain();
      var convolver = ctx.createConvolver();
      convolver.normalize = true;

      var lp = ctx.createBiquadFilter();
      lp.type = 'lowpass'; lp.frequency.value = 3500; lp.Q.value = 1.0;
      var hp = ctx.createBiquadFilter();
      hp.type = 'highpass'; hp.frequency.value = 250; hp.Q.value = 0.8;

      var wetGain = ctx.createGain();
      wetGain.gain.value = 0;
      var dryGain = ctx.createGain();
      dryGain.gain.value = 1;
      var revSum = ctx.createGain();

      revInput.connect(dryGain); dryGain.connect(revSum);
      revInput.connect(hp); hp.connect(lp); lp.connect(convolver);
      convolver.connect(wetGain); wetGain.connect(revSum);
      revSum.connect(revOutput);

      revN = { input: revInput, output: revOutput, convolver: convolver, wetGain: wetGain, dryGain: dryGain };
      prevNode.connect(revN.input);

      outDest = ctx.createMediaStreamDestination();
      revN.output.connect(outDest);

      // 4. Monitor
      micMonitorGain = ctx.createGain();
      micMonitorGain.gain.value = 0;
      revN.output.connect(micMonitorGain);
      try { micMonitorGain.connect(ctx.destination); } catch(e) {}

      // 5. Soundboard
      soundGain = ctx.createGain();
      soundGain.gain.value = cfg.soundActive ? cfg.soundVol : 0;
      soundGain.connect(revN.input);
      var soundMonitorGain = ctx.createGain();
      soundMonitorGain.gain.value = 1.0;
      soundGain.connect(soundMonitorGain);
      try { soundMonitorGain.connect(ctx.destination); } catch(e) {}

      log('AudioContext ready');
      ev('eax-status', { ctx: 'ready' });
      
      regenerateReverbIR();
      return ctx;
    }

    function makeBoostCurve(amount) {
      var n = 2048, c = new Float32Array(n);
      var k = Math.max(0, amount) * 250;
      if (k < 0.5) { for (var i=0;i<n;i++) c[i]=(i*2)/n-1; return c; }
      var deg = Math.PI / 180;
      for (var j=0;j<n;j++){
        var x = (j*2)/n - 1;
        c[j] = ((3 + k) * x * 20 * deg) / (Math.PI + k * Math.abs(x));
      }
      return c;
    }

    function createImpulseResponse(preset, amount) {
      var sampleRate = ctx.sampleRate;
      var baseDuration = 3.0;
      var baseDecay = 3.5;

      if (preset === 'Cathedral') { baseDuration = 8.0; baseDecay = 1.5; }
      else if (preset === 'Chapel') { baseDuration = 6.0; baseDecay = 2.0; }
      else if (preset === 'Church') { baseDuration = 5.0; baseDecay = 2.5; }
      else if (preset === 'Concert Hall') { baseDuration = 4.0; baseDecay = 3.0; }
      else if (preset === 'Garage') { baseDuration = 2.0; baseDecay = 5.0; }
      else if (preset === 'Haunted Cavern') { baseDuration = 6.0; baseDecay = 2.0; }
      else if (preset === 'Indoor Arena') { baseDuration = 5.0; baseDecay = 2.5; }

      var strength = amount / 100; 
      var actualDuration = baseDuration * (0.2 + (strength * 0.8)); 
      var actualDecay = baseDecay * (1.5 - (strength * 0.5)); 

      if (actualDuration < 0.1) actualDuration = 0.1;

      var length = Math.floor(sampleRate * actualDuration);
      var impulseBuffer = ctx.createBuffer(2, length, sampleRate);
      var left = impulseBuffer.getChannelData(0);
      var right = impulseBuffer.getChannelData(1);

      for (var i = 0; i < length; i++) {
        var percent = i / length;
        var formula = Math.pow(1 - percent, actualDecay);
        left[i] = (Math.random() * 2 - 1) * formula;
        right[i] = (Math.random() * 2 - 1) * formula;
      }
      return impulseBuffer;
    }

    function regenerateReverbIR() {
      if (!revN || !ctx) return;
      try {
        revN.convolver.buffer = createImpulseResponse(cfg.eaxPreset, cfg.eaxAmount);
      } catch(e) {}
    }

    function createPitchNode() {
      var processor = ctx.createScriptProcessor(8192, 1, 1);
      var B = 32768; 
      var half = B >> 1;
      var buf = new Float32Array(B);
      var wp = 0, d1 = 0, d2 = half;
      var ratio = Math.pow(2, cfg.pitchSemi / 12);
      
      processor.setSemi = function(s){ ratio = Math.pow(2, s / 12); };
      
      processor.onaudioprocess = function(e) {
        var inp = e.inputBuffer.getChannelData(0);
        var out = e.outputBuffer.getChannelData(0);
        var N = inp.length, dd = 1 - ratio;
        for (var i = 0; i < N; i++) {
          buf[wp] = inp[i];
          var a1 = ((wp - d1) % B + B) % B, a2 = ((wp - d2) % B + B) % B;
          var i1 = Math.floor(a1), i2 = Math.floor(a2);
          var f1 = a1 - i1, f2 = a2 - i2;
          var s1 = buf[i1]*(1-f1) + buf[(i1+1)%B]*f1;
          var s2 = buf[i2]*(1-f2) + buf[(i2+1)%B]*f2;
          var w1 = 1 - Math.abs((d1/half)*2 - 1);
          var w2 = 1 - Math.abs((d2/half)*2 - 1);
          var sw = w1 + w2 + 1e-6;
          out[i] = (s1*w1 + s2*w2)/sw;
          d1 += dd; d2 += dd;
          if (d1 < 0) d1 += half; if (d1 >= half) d1 -= half;
          if (d2 < 0) d2 += half; if (d2 >= half) d2 -= half;
          wp = (wp + 1) % B;
        }
      };
      return processor;
    }

    function buildMic(stream) {
      if (!ensure()) return null;
      var src = ctx.createMediaStreamSource(stream);
      
      var p = createPitchNode();
      var pO = ctx.createGain(); 
      pO.gain.value = cfg.pitchOn ? 1 : 0;
      var sT = ctx.createGain(); 
      sT.gain.value = cfg.pitchOn ? 0 : 1;
      var ap = ctx.createGain();
      
      src.connect(p);
      src.connect(sT);
      p.connect(pO);
      pO.connect(ap);
      sT.connect(ap);

      ap.connect(mixBus);
      
      mic = { src: src, pitch: p, pitchOut: pO, straight: sT, ap: ap };
      ev('eax-status', { mic: 'active' });
      return mic;
    }

    function refresh() {
      if (!ctx) return;

      // Monitor
      if (micMonitorGain) {
        micMonitorGain.gain.value = cfg.monitorOn ? cfg.monitorLevel : 0;
      }

      // EQ
      if (eqNodes[125]) eqNodes[125].gain.value = cfg.eqOn ? cfg.eq125 : 0;
      if (eqNodes[250]) eqNodes[250].gain.value = cfg.eqOn ? cfg.eq250 : 0;
      if (eqNodes[500]) eqNodes[500].gain.value = cfg.eqOn ? cfg.eq500 : 0;
      if (eqNodes[1000]) eqNodes[1000].gain.value = cfg.eqOn ? cfg.eq1k : 0;
      if (eqNodes[2000]) eqNodes[2000].gain.value = cfg.eqOn ? cfg.eq2k : 0;
      if (eqNodes[4000]) eqNodes[4000].gain.value = cfg.eqOn ? cfg.eq4k : 0;
      if (eqNodes[8000]) eqNodes[8000].gain.value = cfg.eqOn ? cfg.eq8k : 0;

      // ML Boost
      if (boostPre && boostPost) {
        if (cfg.boostOn) {
          boostPre.gain.value = cfg.boostGain;
          boostShaper.curve = makeBoostCurve(cfg.boostDrive);
          boostPost.gain.value = 0.5;
        } else {
          boostPre.gain.value = 1.0;
          boostShaper.curve = makeBoostCurve(0);
          boostPost.gain.value = 1.0;
        }
      }

      // EAX
      if (revN) {
        try {
          var now = ctx.currentTime;
          var wet = cfg.eaxOn ? (cfg.eaxAmount / 100) : 0;
          revN.wetGain.gain.setTargetAtTime(wet, now, 0.06);
          revN.dryGain.gain.setTargetAtTime(1.0, now, 0.06);
        } catch(e) {
          revN.wetGain.gain.value = cfg.eaxOn ? (cfg.eaxAmount / 100) : 0;
        }
      }

      // Pitch
      if (mic) {
        mic.pitch.setSemi(cfg.pitchSemi);
        mic.pitchOut.gain.value = cfg.pitchOn ? 1 : 0;
        mic.straight.gain.value = cfg.pitchOn ? 0 : 1;
      }

      // Soundboard
      if (soundGain) {
        soundGain.gain.value = cfg.soundActive ? cfg.soundVol : 0;
      }
    }

    function decodeSound(ab, name) {
      ensure();
      if (!ctx) { ev('eax-sound-err', { m: 'الصوت غير مهيأ' }); return; }
      var done = false;
      function ok(buf) {
        if (done) return; done = true;
        soundBuf = buf; soundName = name || 'ملف';
        ev('eax-sound-loaded', { name: soundName });
      }
      function err(msg) { if (done) return; done = true; ev('eax-sound-err', { m: msg }); }
      try {
        ctx.decodeAudioData(ab.slice(0), ok, function() { err('صيغة غير مدعومة'); });
      } catch (e) { err('فشل فك الترميز'); }
    }

    window.addEventListener('eax-load', function(e) {
      var d = e.detail || {};
      if (!d.ab || !d.ab.byteLength) { ev('eax-sound-err', { m: 'الملف فارغ' }); return; }
      decodeSound(d.ab, d.name);
    });

    window.addEventListener('eax-play', function() {
      if (!soundBuf) { ev('eax-sound-err', { m: 'لا يوجد ملف' }); return; }
      ensure();
      if (!ctx) return;
      var doPlay = function() {
        try { if (soundSrc) { soundSrc.onended = null; soundSrc.stop(); } } catch(e){}
        var src = ctx.createBufferSource();
        src.buffer = soundBuf;
        src.connect(soundGain);
        src.onended = function() { soundSrc = null; ev('eax-sound-ended', {}); };
        try { src.start(0); } catch(e) { ev('eax-sound-err', { m: 'فشل التشغيل' }); return; }
        soundSrc = src;
        ev('eax-sound-started', {});
      };
      if (ctx.state === 'suspended') ctx.resume().then(doPlay).catch(doPlay);
      else doPlay();
    });

    window.addEventListener('eax-stop', function() {
      try { if (soundSrc) { soundSrc.onended = null; soundSrc.stop(); } } catch(e){}
      soundSrc = null;
      ev('eax-sound-stopped', {});
    });

    window.addEventListener('eax-clear', function() {
      try { if (soundSrc) { soundSrc.onended = null; soundSrc.stop(); } } catch(e){}
      soundSrc = null; soundBuf = null; soundName = '';
      ev('eax-sound-cleared', {});
    });

    window.addEventListener('eax-sound-vol', function(e) {
      cfg.soundVol = (e.detail && e.detail.v) || 1.0;
      if (soundGain) soundGain.gain.value = cfg.soundActive ? cfg.soundVol : 0;
    });

    window.addEventListener('eax-sound-active', function(e) {
      cfg.soundActive = !!(e.detail && e.detail.v);
      if (soundGain) soundGain.gain.value = cfg.soundActive ? cfg.soundVol : 0;
    });

    try {
      var md = navigator.mediaDevices;
      if (md && md.getUserMedia) {
        var orig = md.getUserMedia.bind(md);
        md.getUserMedia = function(c) {
          return orig(c).then(function(s) {
            if (!c || !c.audio) return s;
            try {
              ensure();
              buildMic(s);
              var at = outDest.stream.getAudioTracks();
              var vt = s.getVideoTracks();
              if (!at.length) return s;
              var m = new MediaStream();
              at.forEach(function(t){ m.addTrack(t); });
              vt.forEach(function(t){ m.addTrack(t); });
              ev('eax-status', { intercepted: true });
              return m;
            } catch (e) { return s; }
          });
        };
      }
    } catch (e) {}

    window.addEventListener('eax-cfg', function(e) {
      var d = e.detail || {};
      var oldPreset = cfg.eaxPreset;
      var oldAmount = cfg.eaxAmount;
      
      for (var k in d) if (d.hasOwnProperty(k)) cfg[k] = d[k];
      
      if (cfg.eaxPreset !== oldPreset || cfg.eaxAmount !== oldAmount) {
        regenerateReverbIR();
      }
      refresh();
    });

    function onG() { ensure(); }
    window.addEventListener('touchstart', onG, { passive: true });
    window.addEventListener('click', onG);

    ev('eax-status', { core: 'loaded' });
  };

  function injectCore() {
    try {
      var src = '(' + CORE.toString() + ')();';
      var blob = new Blob([src], { type: 'application/javascript' });
      var url = URL.createObjectURL(blob);
      var s = document.createElement('script');
      s.src = url;
      s.onload = function () { try { URL.revokeObjectURL(url); } catch (e) {} };
      var root = document.head || document.documentElement || document.body;
      if (root) root.appendChild(s);
      else setTimeout(injectCore, 10);
    } catch (e) {
      try {
        var s2 = document.createElement('script');
        s2.textContent = '(' + CORE.toString() + ')();';
        (document.head || document.documentElement).appendChild(s2);
        s2.remove();
      } catch (e2) {}
    }
  }
  injectCore();

  var cfg = {
    monitorOn: false, monitorLevel: 0.3,
    eaxOn: true, eaxPreset: 'Haunted Cavern', eaxAmount: 0,
    eqOn: false, eq125: 0, eq250: 0, eq500: 0, eq1k: 0, eq2k: 0, eq4k: 0, eq8k: 0,
    boostOn: true, boostGain: 20.0, boostDrive: 0.85,
    pitchOn: false, pitchSemi: 0,
    soundActive: true, soundVol: 1.0
  };

  var state = { coreHooked: false, coreCtx: 'loading', soundLoaded: false, soundPlaying: false, soundName: '', soundVol: 1.0 };

  function pushCfg(patch) {
    var merged = {};
    for (var k in cfg) merged[k] = cfg[k];
    for (var k2 in patch) merged[k2] = patch[k2];
    window.dispatchEvent(new CustomEvent('eax-cfg', { detail: merged }));
  }

  window.addEventListener('eax-status', function(e) {
    var d = e.detail || {};
    if (d.hooked) state.coreHooked = true;
    if (d.ctx) state.coreCtx = d.ctx;
    updateStatusBadge();
  });

  window.addEventListener('eax-sound-loaded', function(e) {
    state.soundLoaded = true;
    state.soundName = (e.detail && e.detail.name) || 'ملف';
    updateSoundUI();
    showMsg('تم تحميل: ' + state.soundName, true);
  });
  window.addEventListener('eax-sound-err', function(e) { showMsg('خطأ: ' + ((e.detail && e.detail.m) || ''), false); });
  window.addEventListener('eax-sound-started', function() { state.soundPlaying = true; updateSoundUI(); });
  window.addEventListener('eax-sound-stopped', function() { state.soundPlaying = false; updateSoundUI(); });
  window.addEventListener('eax-sound-ended', function() { state.soundPlaying = false; updateSoundUI(); });
  window.addEventListener('eax-sound-cleared', function() {
    state.soundLoaded = false; state.soundPlaying = false; state.soundName = '';
    updateSoundUI();
  });

  function updateStatusBadge() {
    var b = document.querySelector('#eax-badge');
    if (!b) return;
    if (!state.coreHooked) { b.textContent = 'تحميل…'; b.style.background = 'rgba(0,150,255,0.25)'; return; }
    if (state.coreCtx === 'running' || state.coreCtx === 'ready') { b.textContent = 'جاهز'; b.style.background = 'rgba(0,150,255,0.8)'; b.style.color = '#fff'; }
    else { b.textContent = state.coreCtx; b.style.background = 'rgba(255,255,255,0.2)'; }
  }

  function showMsg(msg, ok) {
    var el = document.querySelector('#eax-msg');
    if (!el) return;
    el.textContent = (ok ? '✓ ' : '⚠ ') + msg;
    el.style.background = ok ? 'rgba(0,150,255,0.2)' : 'rgba(255,0,0,0.2)';
    el.style.color = ok ? '#fff' : '#ff6666';
    el.style.border = ok ? '1px solid #0096ff' : '1px solid #ff0000';
    el.style.display = 'block';
    setTimeout(function() { el.style.display = 'none'; }, 3000);
  }

  function updateSoundUI() {
    var noFile = document.querySelector('#eax-no-file');
    var hasFile = document.querySelector('#eax-has-file');
    var nameEl = document.querySelector('#eax-sound-name');
    var volEl = document.querySelector('#eax-sound-vol-val');
    var playBtn = document.querySelector('#eax-play');
    var stopBtn = document.querySelector('#eax-stop');
    if (!noFile || !hasFile) return;
    if (state.soundLoaded) {
      noFile.classList.add('eax-hidden');
      hasFile.classList.remove('eax-hidden');
      if (nameEl) nameEl.textContent = state.soundName || 'ملف';
      if (volEl) volEl.textContent = Math.round(state.soundVol * 100) + '%';
      if (playBtn) playBtn.disabled = state.soundPlaying;
      if (stopBtn) stopBtn.disabled = !state.soundPlaying;
    } else {
      noFile.classList.remove('eax-hidden');
      hasFile.classList.add('eax-hidden');
    }
  }

  function injectUI() {
    if (document.getElementById('eax-panel')) return;
    if (!document.body) return;

    var style = document.createElement('style');
    style.textContent = [
      '#eax-panel,#eax-panel *{box-sizing:border-box;font-family:-apple-system,"SF Pro","Segoe UI",Tahoma,sans-serif}',
      '#eax-panel{position:fixed;bottom:80px;right:10px;width:290px;background:#000;border:1px solid #0096ff;border-radius:12px;box-shadow:0 0 15px rgba(0,150,255,0.6);color:#fff;font-size:11px;z-index:2147483600;direction:rtl;user-select:none;overflow:hidden;}',
      '.eax-head{display:flex;align-items:center;justify-content:space-between;padding:8px 10px;background:#000;border-bottom:1px solid #0096ff;cursor:move;}',
      '.eax-logo{font-weight:800;letter-spacing:1.5px;font-size:12px;color:#0096ff;text-shadow:0 0 5px #0096ff;}',
      '#eax-badge{display:inline-block;font-size:9px;padding:2px 6px;border-radius:20px;background:#0096ff;color:#fff;margin-right:6px;font-weight:600;vertical-align:middle}',
      '.eax-min{background:#111;border:1px solid #0096ff;color:#0096ff;width:20px;height:20px;border-radius:6px;cursor:pointer;font-size:13px;line-height:1;padding:0}',
      '.eax-body{padding:10px;max-height:75vh;overflow-y:auto}',
      '.eax-body.collapsed{display:none}',
      '.eax-sec{margin-top:8px;padding:8px;background:#0a0a0a;border-radius:8px;border:1px solid #333;}',
      '.eax-sec-title{display:flex;align-items:center;justify-content:space-between;margin-bottom:6px;font-size:10px;font-weight:700;color:#fff;border-bottom:1px solid #222;padding-bottom:6px;}',
      '.eax-switch{position:relative;width:28px;height:15px;background:#333;border-radius:20px;cursor:pointer;transition:background .22s;flex-shrink:0}',
      '.eax-switch::after{content:"";position:absolute;top:2px;right:2px;width:11px;height:11px;background:#666;border-radius:50%;transition:all .22s}',
      '.eax-switch.on{background:#0096ff}',
      '.eax-switch.on::after{right:15px;background:#fff;}',
      '.eax-row{display:flex;align-items:center;justify-content:space-between;margin:8px 0;gap:5px}',
      '.eax-row label{font-size:10px;color:#aaa;white-space:nowrap;min-width:60px}',
      '.eax-select{flex:1;background:#000;color:#fff;border:1px solid #0096ff;border-radius:4px;padding:4px;font-size:10px;outline:none;}',
      '.eax-val{font-size:9px;color:#0096ff;min-width:34px;text-align:left;font-weight:600;}',
      '.eax-range{-webkit-appearance:none;appearance:none;flex:1;height:4px;background:#333;border-radius:4px;outline:none;direction:ltr}',
      '.eax-range::-webkit-slider-thumb{-webkit-appearance:none;width:14px;height:14px;background:#fff;border-radius:50%;cursor:pointer;box-shadow:0 0 5px #0096ff;}',
      
      '.eax-eq-grid{display:grid;grid-template-columns:repeat(7,1fr);gap:4px;height:120px;align-items:end;margin-top:10px}',
      '.eax-eq-col{display:flex;flex-direction:column;align-items:center;height:100%}',
      '.eax-eq-slider{-webkit-appearance:none;appearance:none;width:6px;height:100%;background:#222;border-radius:4px;outline:none;writing-mode:vertical-lr;direction:rtl}',
      '.eax-eq-slider::-webkit-slider-thumb{-webkit-appearance:none;width:12px;height:12px;background:#0096ff;border-radius:50%;cursor:pointer;border:1px solid #fff; box-shadow:0 0 5px #0096ff;}',
      '.eax-eq-label{font-size:8px;color:#888;margin-top:4px}',
      
      '.eax-foot{margin-top:10px;text-align:center;font-size:9px;color:#0096ff;font-weight:700;text-shadow:0 0 5px #0096ff;}',
      '.eax-btn{flex:1;padding:8px;background:#000;border:1px solid #0096ff;color:#fff;border-radius:6px;cursor:pointer;font-size:10px; font-weight:bold;}',
      '.eax-btn:hover{background:#0096ff;}',
      '.eax-file-row{display:flex;gap:6px;margin-top:5px}',
      '#eax-msg{margin-top:8px;padding:6px;border-radius:5px;font-size:10px;display:none;text-align:center;}',
      '.eax-hidden{display:none!important}'
    ].join('');
    document.head.appendChild(style);

    var panel = document.createElement('div');
    panel.id = 'eax-panel';
    panel.innerHTML =
      '<div class="eax-head" id="eax-head" style="cursor: move;">' +
        '<div class="eax-logo">EAX <span id="eax-badge">تحميل…</span></div>' +
        '<button class="eax-min" id="eax-min">−</button>' +
      '</div>' +
      '<div class="eax-body" id="eax-body">' +

        '<div class="eax-sec">' +
          '<div class="eax-sec-title"><span>🎧 اسمع نفسي</span><div class="eax-switch" data-k="monitorOn"></div></div>' +
          '<div class="eax-row"><label>المستوى</label><input class="eax-range" type="range" min="0" max="1" step="0.01" value="'+cfg.monitorLevel+'" data-k="monitorLevel"><span class="eax-val">'+Math.round(cfg.monitorLevel*100)+'%</span></div>' +
        '</div>' +

        '<div class="eax-sec">' +
          '<div class="eax-sec-title"><span>🎭 نبرة الصوت</span><div class="eax-switch" data-k="pitchOn"></div></div>' +
          '<div class="eax-row"><label>النغمة</label><input class="eax-range" type="range" min="-12" max="12" step="1" value="'+cfg.pitchSemi+'" data-k="pitchSemi"><span class="eax-val">'+cfg.pitchSemi+'</span></div>' +
        '</div>' +

        '<div class="eax-sec">' +
          '<div class="eax-sec-title"><span>EAX Effects</span><div class="eax-switch on" data-k="eaxOn"></div></div>' +
          '<div class="eax-row"><label>Environment</label><select class="eax-select" data-k="eaxPreset">' +
            '<option value="Cathedral">Cathedral</option>' +
            '<option value="Chapel">Chapel</option>' +
            '<option value="Church">Church</option>' +
            '<option value="Concert Hall">Concert Hall</option>' +
            '<option value="Garage">Garage</option>' +
            '<option value="Haunted Cavern" selected>Haunted Cavern</option>' +
            '<option value="Indoor Arena">Indoor Arena</option>' +
          '</select></div>' +
          '<div class="eax-row"><label>Amount</label><input class="eax-range" type="range" min="0" max="100" step="1" value="'+cfg.eaxAmount+'" data-k="eaxAmount"><span class="eax-val">'+cfg.eaxAmount+'%</span></div>' +
        '</div>' +

        // EQ + ML Boost
        '<div class="eax-sec">' +
          '<div class="eax-sec-title"><span>Equalizer</span><div class="eax-switch" data-k="eqOn"></div></div>' +
          
          '<div class="eax-eq-grid">' +
            '<div class="eax-eq-col"><input class="eax-eq-slider" type="range" min="-12" max="12" step="1" value="0" data-k="eq125"><div class="eax-eq-label">125</div></div>' +
            '<div class="eax-eq-col"><input class="eax-eq-slider" type="range" min="-12" max="12" step="1" value="0" data-k="eq250"><div class="eax-eq-label">250</div></div>' +
            '<div class="eax-eq-col"><input class="eax-eq-slider" type="range" min="-12" max="12" step="1" value="0" data-k="eq500"><div class="eax-eq-label">500</div></div>' +
            '<div class="eax-eq-col"><input class="eax-eq-slider" type="range" min="-12" max="12" step="1" value="0" data-k="eq1k"><div class="eax-eq-label">1k</div></div>' +
            '<div class="eax-eq-col"><input class="eax-eq-slider" type="range" min="-12" max="12" step="1" value="0" data-k="eq2k"><div class="eax-eq-label">2k</div></div>' +
            '<div class="eax-eq-col"><input class="eax-eq-slider" type="range" min="-12" max="12" step="1" value="0" data-k="eq4k"><div class="eax-eq-label">4k</div></div>' +
            '<div class="eax-eq-col"><input class="eax-eq-slider" type="range" min="-12" max="12" step="1" value="0" data-k="eq8k"><div class="eax-eq-label">8k</div></div>' +
          '</div>' +

          // ✅ ML Boost (شريط واحد)
          '<div class="eax-sec-title" style="margin-top:10px; border-top:1px solid #222; padding-top:8px;">' +
             '<span>ML Boost</span>' +
             '<div class="eax-switch on" data-k="boostOn"></div>' +
          '</div>' +
          '<div class="eax-row"><label>القوة</label><input class="eax-range" type="range" min="1" max="100" step="1" value="'+cfg.boostGain+'" data-k="boostGain"><span class="eax-val">'+cfg.boostGain.toFixed(0)+'x</span></div>' +
          '<div class="eax-row"><label>تشويش</label><input class="eax-range" type="range" min="0" max="1" step="0.01" value="'+cfg.boostDrive+'" data-k="boostDrive"><span class="eax-val">'+Math.round(cfg.boostDrive*100)+'%</span></div>' +

        '</div>' +

        // Soundboard
        '<div class="eax-sec">' +
          '<div class="eax-sec-title"><span>Soundboard</span><div class="eax-switch on" id="eax-sound-toggle"></div></div>' +
          // ✅ نقبل audio + video
          '<input type="file" id="eax-file" accept="audio/*,video/*,.mp3,.wav,.m4a,.aac,.ogg,.flac,.opus,.mp4,.mov,.avi,.mkv" class="eax-hidden">' +
          '<div id="eax-no-file"><div class="eax-file-row"><button class="eax-btn" id="eax-pick">اختر ملف صوتي أو فيديو</button></div></div>' +
          '<div id="eax-has-file" class="eax-hidden">' +
            '<div class="eax-sound-info" style="display:flex; justify-content:space-between; margin-bottom:5px; color:#0096ff; font-size:10px;">' +
               '<span id="eax-sound-name">-</span>' +
               '<span id="eax-sound-vol-val">100%</span>' +
            '</div>' +
            '<div class="eax-file-row">' +
               '<button class="eax-btn" style="color:#86efac;" id="eax-play">▶ تشغيل</button>' +
               '<button class="eax-btn" style="color:#fca5a5;" id="eax-stop">■ إيقاف</button>' +
            '</div>' +
            '<div class="eax-row"><label>الصوت</label><input class="eax-range" type="range" min="0" max="2" step="0.05" value="1" id="eax-sound-vol"><span class="eax-val" id="eax-sound-vol-2">100%</span></div>' +
            '<div class="eax-file-row"><button class="eax-btn" style="border-color:#ff0000; color:#ff0000;" id="eax-delete">🗑 حذف</button></div>' +
          '</div>' +
          '<div id="eax-msg"></div>' +
        '</div>' +

        '<div class="eax-foot">EAX made by سكونا العتيبي - Nzf</div>' +
      '</div>';

    document.body.appendChild(panel);

    // Drag Logic
    var drag = false, sx = 0, sy = 0, ox = 0, oy = 0;
    var header = panel.querySelector('#eax-head');
    
    header.addEventListener('mousedown', function(e) {
      drag = true; 
      sx = e.clientX; 
      sy = e.clientY;
      var r = panel.getBoundingClientRect();
      ox = r.left; 
      oy = r.top;
      panel.style.right = 'auto'; 
      panel.style.bottom = 'auto';
      panel.style.left = ox + 'px'; 
      panel.style.top = oy + 'px';
      e.preventDefault();
    });

    window.addEventListener('mousemove', function(e) {
      if (!drag) return;
      panel.style.left = (ox + e.clientX - sx) + 'px';
      panel.style.top = (oy + e.clientY - sy) + 'px';
    });

    window.addEventListener('mouseup', function() { drag = false; });

    // Logic
    var body = panel.querySelector('#eax-body');
    panel.querySelector('#eax-min').onclick = function(e) {
      e.stopPropagation();
      body.classList.toggle('collapsed');
      panel.querySelector('#eax-min').textContent = body.classList.contains('collapsed') ? '+' : '−';
    };

    panel.querySelectorAll('.eax-switch').forEach(function(sw) {
      if (sw.id === 'eax-sound-toggle') return;
      sw.onclick = function(e) {
        e.stopPropagation();
        sw.classList.toggle('on');
        var k = sw.dataset.k;
        cfg[k] = sw.classList.contains('on');
        var patch = {}; patch[k] = cfg[k];
        pushCfg(patch);
      };
    });

    panel.querySelectorAll('.eax-range').forEach(function(inp) {
      if (inp.id === 'eax-sound-vol') return;
      var k = inp.dataset.k;
      if (!k) return;
      inp.oninput = function() {
        cfg[k] = parseFloat(inp.value);
        var v = inp.parentElement.querySelector('.eax-val');
        if (v) v.textContent = inp.value + (k.includes('Level')||k==='eaxAmount'?'%':(k==='boostGain'?'x':''));
        var patch = {}; patch[k] = cfg[k];
        pushCfg(patch);
      };
    });

    panel.querySelectorAll('.eax-eq-slider').forEach(function(inp) {
      var k = inp.dataset.k;
      inp.oninput = function() {
        cfg[k] = parseFloat(inp.value);
        var patch = {}; patch[k] = cfg[k];
        pushCfg(patch);
      };
    });

    var select = panel.querySelector('select[data-k="eaxPreset"]');
    select.onchange = function() {
      cfg.eaxPreset = select.value;
      pushCfg({ eaxPreset: select.value });
    };

    var fileInput = panel.querySelector('#eax-file');
    var soundVolSlider = panel.querySelector('#eax-sound-vol');
    var soundVolLabel2 = panel.querySelector('#eax-sound-vol-2');
    
    panel.querySelector('#eax-pick').onclick = function(e) { e.stopPropagation(); fileInput.click(); };
    
    // ✅ يقبل صوت + فيديو
    fileInput.onchange = function(e) {
      var f = e.target.files && e.target.files[0];
      if (!f) return;
      window.dispatchEvent(new CustomEvent('eax-resume'));
      if (f.size > 40 * 1024 * 1024) {
        showMsg('الملف كبير جداً (الحد 40 ميجا)', false);
        fileInput.value = ''; return;
      }
      var reader = new FileReader();
      reader.onload = function(ev2) {
        var ab = ev2.target.result;
        if (!ab || !ab.byteLength) { showMsg('الملف فارغ', false); return; }
        state.soundVol = 1.0;
        if (soundVolSlider) soundVolSlider.value = 1.0;
        if (soundVolLabel2) soundVolLabel2.textContent = '100%';
        window.dispatchEvent(new CustomEvent('eax-load', { detail: { ab: ab, name: f.name } }));
      };
      reader.onerror = function() { showMsg('فشل قراءة الملف', false); };
      try { reader.readAsArrayBuffer(f); } catch (err) { showMsg('فشل قراءة الملف', false); }
      fileInput.value = '';
    };

    panel.querySelector('#eax-play').onclick = function(e) { e.stopPropagation(); window.dispatchEvent(new CustomEvent('eax-play')); };
    panel.querySelector('#eax-stop').onclick = function(e) { e.stopPropagation(); window.dispatchEvent(new CustomEvent('eax-stop')); };
    // ✅ زر الحذف
    panel.querySelector('#eax-delete').onclick = function(e) { e.stopPropagation(); window.dispatchEvent(new CustomEvent('eax-clear')); };

    panel.querySelector('#eax-sound-toggle').onclick = function(e) {
      e.stopPropagation();
      this.classList.toggle('on');
      window.dispatchEvent(new CustomEvent('eax-sound-active', { detail: { v: this.classList.contains('on') } }));
    };

    soundVolSlider.oninput = function() {
      var v = parseFloat(soundVolSlider.value);
      state.soundVol = v;
      if (soundVolLabel2) soundVolLabel2.textContent = Math.round(v * 100) + '%';
      window.dispatchEvent(new CustomEvent('eax-sound-vol', { detail: { v: v } }));
    };

    updateStatusBadge();
    updateSoundUI();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', injectUI);
  else injectUI();

  var mo = new MutationObserver(function() { if (!document.getElementById('eax-panel')) injectUI(); });
  window.addEventListener('load', function() { mo.observe(document.body, { childList: true }); });

})();