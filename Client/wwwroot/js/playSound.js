window.soundPlayer = {
  playStoneSound: function () {
    let stoneSound = document.getElementById('stoneSound');
    if (stoneSound) {
      stoneSound.load();
      stoneSound.volume = 0.5;
      stoneSound.play();
    }
  }
}

