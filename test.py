import random
import time
import pandas as pd

# 맵 크기
MAP_SIZE = 10

# 요소 개수 설정
NUM_WAVES = 10
NUM_TRASH = 10
NUM_OBSTACLES = 10

# 환경 요소 랜덤 생성
def generate_map():
    wave = [[0 for _ in range(MAP_SIZE)] for _ in range(MAP_SIZE)]
    trash = [[0 for _ in range(MAP_SIZE)] for _ in range(MAP_SIZE)]
    obstacle = [[0 for _ in range(MAP_SIZE)] for _ in range(MAP_SIZE)]

    for _ in range(NUM_WAVES):
        x, y = random.randint(0, MAP_SIZE - 1), random.randint(0, MAP_SIZE - 1)
        wave[x][y] = random.choice([1.5, 2.0, 2.5])

    for _ in range(NUM_TRASH):
        x, y = random.randint(0, MAP_SIZE - 1), random.randint(0, MAP_SIZE - 1)
        trash[x][y] = 1

    for _ in range(NUM_OBSTACLES):
        x, y = random.randint(0, MAP_SIZE - 1), random.randint(0, MAP_SIZE - 1)
        obstacle[x][y] = 1

    return wave, trash, obstacle

# 맵 출력 함수
def print_map(title, data):
    print(f"\n🌊 {title}")
    df = pd.DataFrame(data)
    print(df)

# 탐색 시뮬레이션
def run_greedy_algorithm(wave, trash, obstacle):
    visited = [[False for _ in range(MAP_SIZE)] for _ in range(MAP_SIZE)]
    x, y = 0, 0  # 시작 위치
    collected = 0
    total_trash = sum([row.count(1) for row in trash])
    collisions = 0
    path_length = 0

    start = time.time()

    while collected < total_trash:
        visited[x][y] = True
        if trash[x][y] == 1:
            collected += 1
            trash[x][y] = 0

        # 상하좌우 이동 가능한 곳 찾기
        directions = [(0,1), (1,0), (0,-1), (-1,0)]
        next_moves = []

        for dx, dy in directions:
            nx, ny = x + dx, y + dy
            if 0 <= nx < MAP_SIZE and 0 <= ny < MAP_SIZE and not visited[nx][ny]:
                cost = 1 + wave[nx][ny]  # 파도 영향
                if obstacle[nx][ny]:
                    cost += 5  # 충돌 위험이 큰 곳은 비우선
                next_moves.append((cost, nx, ny))

        if not next_moves:
            break  # 이동 불가능

        # 가장 cost 낮은 쪽으로 이동 (Greedy)
        next_moves.sort()
        _, x, y = next_moves[0]
        path_length += 1

        if obstacle[x][y]:
            collisions += 1

    end = time.time()
    elapsed = round(end - start, 3)

    return {
        "수거율": round((collected / total_trash) * 100, 2) if total_trash > 0 else 0,
        "충돌 횟수": collisions,
        "탐색 거리": path_length,
        "소요 시간": elapsed
    }

# 실행
if __name__ == "__main__":
    wave, trash, obstacle = generate_map()
    print_map("🌊 Wave Map", wave)
    print_map("🗑 Trash Map", trash)
    print_map("🚧 Obstacle Map", obstacle)

    result = run_greedy_algorithm(wave, trash, obstacle)

    print("\n✅ [시뮬레이션 결과]")
    for key, value in result.items():
        print(f"{key}: {value}")


# 