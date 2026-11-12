// Equipment, combat actors and region routes. Events are authored in events.mjs.
export default {
  "weapons": [
    {
      "id": "sword",
      "name": "낡은 철검",
      "type": "검",
      "icon": "sword",
      "power": 5,
      "speed": 1,
      "rarity": "일반",
      "desc": "무딘 칼날에도 첫 모험의 무게가 실린다.",
      "skills": [
        "slash",
        "shield"
      ]
    },
    {
      "id": "bow",
      "name": "물푸레나무 활",
      "type": "활",
      "icon": "bow",
      "power": 5,
      "speed": 3,
      "rarity": "고급",
      "desc": "뒷줄에서 적의 후열을 노리는 사냥꾼의 활.",
      "skills": [
        "shot",
        "pin"
      ]
    },
    {
      "id": "staff",
      "name": "푸른 잔불 지팡이",
      "type": "지팡이",
      "icon": "spark",
      "power": 6,
      "speed": 0,
      "rarity": "고급",
      "desc": "후열에서 불꽃을 다루고 상처를 봉합한다.",
      "skills": [
        "ember",
        "mend"
      ]
    },
    {
      "id": "dagger",
      "name": "은빛 단검",
      "type": "단검",
      "icon": "sword",
      "power": 4,
      "speed": 5,
      "rarity": "고급",
      "desc": "빠른 찌르기로 앞줄에 출혈을 남긴다.",
      "skills": [
        "stab",
        "feint"
      ]
    },
    {
      "id": "axe",
      "name": "산지기의 도끼",
      "type": "도끼",
      "icon": "axe",
      "power": 8,
      "speed": -1,
      "rarity": "고급",
      "desc": "무겁지만 강하다. 앞줄에서 적을 기절시킨다.",
      "skills": [
        "cleave",
        "bash"
      ]
    },
    {
      "id": "spear",
      "name": "황동 장창",
      "type": "창",
      "icon": "sword",
      "power": 6,
      "speed": 2,
      "rarity": "고급",
      "desc": "중열에서도 적의 앞줄에 닿는 긴 무기.",
      "skills": [
        "thrust",
        "pin"
      ]
    },
    {
      "id": "dawn",
      "name": "여명의 검",
      "type": "검",
      "icon": "sun",
      "power": 11,
      "speed": 3,
      "rarity": "유물",
      "desc": "지켜낸 약속이 별빛처럼 칼날 위에 남는다.",
      "skills": [
        "slash",
        "radiance",
        "shield"
      ]
    }
  ],
  "skills": [
    {
      "id": "slash",
      "name": "가르기",
      "desc": "앞줄의 적을 벤다.",
      "icon": "sword",
      "from": [
        1,
        2
      ],
      "to": [
        1,
        2
      ],
      "power": 1,
      "accuracy": 95,
      "cost": 0
    },
    {
      "id": "shield",
      "name": "검의 수비",
      "desc": "다음 자신의 차례까지 받는 피해 65% 감소. 기력 +1.",
      "icon": "shield",
      "from": [
        1,
        2,
        3,
        4
      ],
      "target": "self",
      "guard": true,
      "cost": 0
    },
    {
      "id": "shot",
      "name": "조준 사격",
      "desc": "어느 열의 적이든 겨눈다.",
      "icon": "bow",
      "from": [
        2,
        3,
        4
      ],
      "to": [
        1,
        2,
        3,
        4
      ],
      "power": 1.05,
      "accuracy": 95,
      "cost": 0
    },
    {
      "id": "pin",
      "name": "발 묶기",
      "desc": "피해를 주고 75% 확률로 한 차례 기절시킨다.",
      "icon": "target",
      "from": [
        2,
        3,
        4
      ],
      "to": [
        1,
        2,
        3
      ],
      "power": 0.65,
      "accuracy": 90,
      "cost": 1,
      "stun": 75
    },
    {
      "id": "ember",
      "name": "불씨 화살",
      "desc": "후열의 적까지 닿는 마법. 방어를 무시한다.",
      "icon": "flame",
      "from": [
        2,
        3,
        4
      ],
      "to": [
        1,
        2,
        3,
        4
      ],
      "power": 1.15,
      "accuracy": 95,
      "cost": 0,
      "magic": true
    },
    {
      "id": "mend",
      "name": "상처 봉합",
      "desc": "아군 한 명의 체력을 10~14 회복하고 출혈을 지운다.",
      "icon": "heart",
      "from": [
        2,
        3,
        4
      ],
      "target": "ally",
      "heal": 12,
      "cost": 2
    },
    {
      "id": "stab",
      "name": "깊은 찌르기",
      "desc": "앞줄에 출혈을 남긴다. 3차례 동안 체력 -2.",
      "icon": "sword",
      "from": [
        1,
        2
      ],
      "to": [
        1,
        2
      ],
      "power": 0.8,
      "accuracy": 95,
      "cost": 0,
      "bleed": 3
    },
    {
      "id": "feint",
      "name": "빈틈 찌르기",
      "desc": "빠르게 치고 빠진다. 자신에게 방어 부여.",
      "icon": "wind",
      "from": [
        1,
        2,
        3
      ],
      "to": [
        1,
        2,
        3
      ],
      "power": 1.1,
      "accuracy": 90,
      "cost": 1,
      "guard": true
    },
    {
      "id": "cleave",
      "name": "내려찍기",
      "desc": "앞줄에 강력한 일격을 가한다.",
      "icon": "axe",
      "from": [
        1,
        2
      ],
      "to": [
        1,
        2
      ],
      "power": 1.2,
      "accuracy": 85,
      "cost": 0
    },
    {
      "id": "bash",
      "name": "충격타",
      "desc": "65% 확률로 한 차례 기절시킨다.",
      "icon": "star",
      "from": [
        1,
        2
      ],
      "to": [
        1,
        2
      ],
      "power": 0.8,
      "accuracy": 90,
      "cost": 1,
      "stun": 65
    },
    {
      "id": "thrust",
      "name": "길게 찌르기",
      "desc": "중열에서 적의 전열을 공격한다.",
      "icon": "sword",
      "from": [
        1,
        2,
        3
      ],
      "to": [
        1,
        2
      ],
      "power": 1,
      "accuracy": 95,
      "cost": 0
    },
    {
      "id": "radiance",
      "name": "새벽의 일격",
      "desc": "모든 열에 닿는 빛. 높은 피해를 준다.",
      "icon": "sun",
      "from": [
        1,
        2,
        3,
        4
      ],
      "to": [
        1,
        2,
        3,
        4
      ],
      "power": 1.6,
      "accuracy": 100,
      "cost": 2,
      "magic": true
    },
    {
      "id": "rally",
      "name": "마음 다잡기",
      "desc": "아군 한 명의 긴장을 18 낮추고 기력을 1 회복한다.",
      "icon": "leaf",
      "from": [
        1,
        2,
        3,
        4
      ],
      "target": "ally",
      "calm": 18,
      "cost": 1
    },
    {
      "id": "focus",
      "name": "숨 고르기",
      "desc": "자신의 기력 +2, 긴장 -6. 차례를 소모한다.",
      "icon": "wind",
      "from": [
        1,
        2,
        3,
        4
      ],
      "target": "self",
      "focus": true,
      "cost": 0
    },
    {
      "id": "guard",
      "name": "방어",
      "desc": "다음 자신의 차례까지 받는 피해 65% 감소. 기력 +1.",
      "icon": "shield",
      "from": [
        1,
        2,
        3,
        4
      ],
      "target": "self",
      "guard": true,
      "cost": 0
    },
    {
      "id": "potion",
      "name": "회복약",
      "desc": "아군 체력 18 회복, 출혈 제거. 회복약 1개 소모.",
      "icon": "potion",
      "from": [
        1,
        2,
        3,
        4
      ],
      "target": "ally",
      "heal": 18,
      "cost": 0,
      "item": "potions"
    },
    {
      "id": "move",
      "name": "자리 바꾸기",
      "desc": "선택한 동료와 자리를 교대한다. 차례를 소모한다.",
      "icon": "swap",
      "from": [
        1,
        2,
        3,
        4
      ],
      "target": "otherAlly",
      "move": true,
      "cost": 0
    },
    {
      "id": "strike",
      "name": "기본 공격",
      "desc": "무기와 위치에 관계없이 사용할 수 있는 약한 공격.",
      "icon": "sword",
      "from": [
        1,
        2,
        3,
        4
      ],
      "to": [
        1,
        2,
        3,
        4
      ],
      "power": 0.65,
      "accuracy": 100,
      "cost": 0
    }
  ],
  "companions": [
    {
      "id": "mira",
      "name": "미라",
      "subtitle": "숲길의 길잡이",
      "weapon": "bow",
      "maxHp": 28,
      "speed": 5,
      "power": 1,
      "portrait": "ranger",
      "skills": [
        "rally"
      ],
      "story": "발목에 붕대를 감은 사냥꾼. 당신이 내민 손을 기억한다."
    },
    {
      "id": "elian",
      "name": "엘리안",
      "subtitle": "떠도는 수련자",
      "weapon": "staff",
      "maxHp": 26,
      "speed": 2,
      "power": 1,
      "portrait": "mage",
      "skills": [
        "rally"
      ],
      "story": "사원보다 길 위에 구할 사람이 많다고 믿는다."
    },
    {
      "id": "borin",
      "name": "보린",
      "subtitle": "산맥의 방패",
      "weapon": "axe",
      "maxHp": 42,
      "speed": 1,
      "power": 2,
      "portrait": "knight",
      "skills": [],
      "story": "무뚝뚝한 전직 호위병. 지킬 이유를 다시 찾고 있다."
    }
  ],
  "enemies": [
    {
      "id": "wolf",
      "name": "굶주린 늑대",
      "portrait": "wolf",
      "maxHp": 16,
      "power": 4,
      "speed": 4,
      "armor": 0,
      "attack": "물어뜯기",
      "bleed": true
    },
    {
      "id": "bandit",
      "name": "길목의 도적",
      "portrait": "bandit",
      "maxHp": 23,
      "power": 6,
      "speed": 3,
      "armor": 1,
      "attack": "녹슨 칼날"
    },
    {
      "id": "archer",
      "name": "도적 궁수",
      "portrait": "archer",
      "maxHp": 17,
      "power": 5,
      "speed": 5,
      "armor": 0,
      "attack": "불길한 화살",
      "ranged": true
    },
    {
      "id": "skeleton",
      "name": "깨어난 망자",
      "portrait": "skeleton",
      "maxHp": 25,
      "power": 7,
      "speed": 2,
      "armor": 2,
      "attack": "묘지의 검"
    },
    {
      "id": "cultist",
      "name": "잿빛 사제",
      "portrait": "mage",
      "maxHp": 23,
      "power": 7,
      "speed": 4,
      "armor": 0,
      "attack": "마음의 균열",
      "ranged": true,
      "stress": 10
    },
    {
      "id": "warden",
      "name": "폐허의 파수꾼",
      "portrait": "knight",
      "maxHp": 75,
      "power": 11,
      "speed": 2,
      "armor": 3,
      "attack": "무너지는 철퇴"
    },
    {
      "id": "dragon",
      "name": "잿빛 날개의 주인",
      "portrait": "dragon",
      "maxHp": 140,
      "power": 14,
      "speed": 5,
      "armor": 3,
      "attack": "재의 숨결",
      "ranged": true,
      "stress": 12
    },
    {
      "id": "slime",
      "name": "유리빛 점액체",
      "portrait": "slime",
      "maxHp": 14,
      "power": 3,
      "speed": 1,
      "armor": 0,
      "attack": "산성 방울"
    },
    {
      "id": "boar",
      "name": "성난 멧돼지",
      "portrait": "boar",
      "maxHp": 22,
      "power": 5,
      "speed": 3,
      "armor": 1,
      "attack": "돌진"
    },
    {
      "id": "spider",
      "name": "갈대 거미",
      "portrait": "spider",
      "maxHp": 19,
      "power": 5,
      "speed": 5,
      "armor": 0,
      "attack": "독니",
      "bleed": true
    },
    {
      "id": "wisp",
      "name": "길 잃은 불빛",
      "portrait": "wisp",
      "maxHp": 21,
      "power": 6,
      "speed": 5,
      "armor": 0,
      "attack": "서늘한 속삭임",
      "ranged": true,
      "stress": 7
    }
  ],
  "regions": [
    {
      "id": "village",
      "name": "브린 마을",
      "description": "익숙한 문턱에도 남의 사정은 숨어 있다.",
      "art": "village",
      "minStage": 0,
      "routes": [
        "forest",
        "road",
        "river"
      ]
    },
    {
      "id": "forest",
      "name": "이끼 숲",
      "description": "나무 사이에서는 약속도 길을 잃는다.",
      "art": "forest",
      "minStage": 0,
      "routes": [
        "village",
        "road",
        "swamp"
      ]
    },
    {
      "id": "road",
      "name": "세 갈래 교역로",
      "description": "서로 다른 목적지가 잠시 같은 길을 쓴다.",
      "art": "village",
      "minStage": 0,
      "routes": [
        "village",
        "forest",
        "coast",
        "mountain",
        "citadel"
      ]
    },
    {
      "id": "river",
      "name": "은빛 강 유역",
      "description": "강물은 물건과 소문을 같은 속도로 운반하지 않는다.",
      "art": "forest",
      "minStage": 0,
      "routes": [
        "village",
        "coast",
        "swamp",
        "ruins"
      ]
    },
    {
      "id": "coast",
      "name": "안개 항구",
      "description": "돌아오는 배와 돌아오지 않는 이름을 센다.",
      "art": "village",
      "minStage": 0,
      "routes": [
        "road",
        "river",
        "citadel"
      ]
    },
    {
      "id": "mountain",
      "name": "서리등 산맥",
      "description": "눈 아래 묻힌 길을 아는 사람은 많지 않다.",
      "art": "forest",
      "minStage": 1,
      "routes": [
        "road",
        "ruins",
        "frontier"
      ]
    },
    {
      "id": "ruins",
      "name": "별무리 유적",
      "description": "옛 왕국은 무너져도 그 빚은 남는다.",
      "art": "ruins",
      "minStage": 1,
      "routes": [
        "river",
        "mountain",
        "citadel",
        "frontier"
      ]
    },
    {
      "id": "swamp",
      "name": "검은갈대 습지",
      "description": "안개 속에서는 발자국보다 기억을 믿어야 한다.",
      "art": "ruins",
      "minStage": 1,
      "routes": [
        "forest",
        "river",
        "ruins"
      ]
    },
    {
      "id": "citadel",
      "name": "새벽 성채",
      "description": "높은 성벽은 안쪽의 불안도 가둔다.",
      "art": "village",
      "minStage": 1,
      "routes": [
        "road",
        "coast",
        "ruins",
        "frontier"
      ]
    },
    {
      "id": "frontier",
      "name": "재의 변경",
      "description": "전설을 끝내도 이곳에는 살아갈 사람이 남는다.",
      "art": "battle",
      "minStage": 2,
      "routes": [
        "mountain",
        "ruins",
        "citadel"
      ]
    }
  ]
};
