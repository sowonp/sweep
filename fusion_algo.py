import pandas as pd
import random
import numpy as np
import time
import heapq
import math
import os
import glob

# ================================================
# 작업 디렉토리 설정
# ================================================
os.chdir("C:/sweep_algorithm_test/")
os.makedirs("algo_test_results", exist_ok=True)

# ================================================
# CSV 데이터 로드
# ================================================
def load_data(file_path):
    return pd.read_csv(file_path)

data = load_data('2023.csv')

# ================================================
# 환경 기반 쓰레기 및 장애물 생성 함수
# ================================================
def generate_trash_obstacles_with_environment(grid_size, env_row):
    # === 환경 데이터 읽기 ===
    wind_speed = env_row['풍속(m/s)']
    wind_direction = env_row['풍향(deg)']
    gust_speed = env_row['GUST풍속(m/s)']
    max_wave_height = env_row['최대파고(m)']
    significant_wave_height = env_row['유의파고(m)']
    mean_wave_height = env_row['평균파고(m)']
    wave_period = env_row['파주기(sec)']
    wave_direction = env_row['파향(deg)']

    # === NaN 처리 (기본값 설정) ===
    if pd.isna(wind_speed):
        wind_speed = 0
    if pd.isna(wind_direction):
        wind_direction = 0
    if pd.isna(gust_speed):
        gust_speed = 0
    if pd.isna(max_wave_height):
        max_wave_height = 0
    if pd.isna(significant_wave_height):
        significant_wave_height = 0
    if pd.isna(mean_wave_height):
        mean_wave_height = 0
    if pd.isna(wave_period):
        wave_period = 0
    if pd.isna(wave_direction):
        wave_direction = 0

    # === 쓰레기와 장애물 개수 결정 ===
    base_trash = 50
    base_obstacles = 10

    trash_count = base_trash + int(wind_speed * 5) + int(significant_wave_height * 10)
    obstacle_count = base_obstacles + int(gust_speed * 2) + int(max_wave_height * 5)

    trash_count = min(trash_count, 300)
    obstacle_count = min(obstacle_count, 50)

    # === 쓰레기 배치 (바람 방향 고려) ===
    trash_positions = []
    for _ in range(trash_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)

        offset_distance = random.randint(0, 5)
        offset_x = int(np.cos(np.radians(wind_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wind_direction)) * offset_distance)

        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))

        trash_positions.append((new_x, new_y))

    # === 장애물 배치 (파향 고려) ===
    obstacle_positions = []
    for _ in range(obstacle_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)

        offset_distance = random.randint(0, 3)
        offset_x = int(np.cos(np.radians(wave_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wave_direction)) * offset_distance)

        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))

        obstacle_positions.append((new_x, new_y))

    return trash_positions, obstacle_positions

# ================================================
# Fusion
# ================================================
# 유클리디안 거리
def distance(a, b):
    return math.sqrt((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2)

# 장애물 여부 확인
def is_collision(pos, obstacle_positions):
    return pos in obstacle_positions

# 인접한 4방향 좌표 반환
def neighbors(pos, grid_size):
    x, y = pos
    directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]
    result = []
    for dx, dy in directions:
        nx, ny = x + dx, y + dy
        if 0 <= nx < grid_size and 0 <= ny < grid_size:
            result.append((nx, ny))
    return result

# A* 알고리즘
def a_star_search(start, goal, obstacle_positions, grid_size):
    open_set = []
    heapq.heappush(open_set, (0 + distance(start, goal), 0, start))
    came_from = {}
    g_score = {start: 0}

    while open_set:
        _, current_cost, current = heapq.heappop(open_set)

        if current == goal:
            return reconstruct_path(came_from, current)

        for neighbor in neighbors(current, grid_size):
            if is_collision(neighbor, obstacle_positions):
                continue

            tentative_g = g_score[current] + 1
            if neighbor not in g_score or tentative_g < g_score[neighbor]:
                came_from[neighbor] = current
                g_score[neighbor] = tentative_g
                f_score = tentative_g + distance(neighbor, goal)
                heapq.heappush(open_set, (f_score, tentative_g, neighbor))

    return None  # 경로 없음

# Dijkstra 알고리즘
def dijkstra_search(start, goal, obstacle_positions, grid_size):
    heap = [(0, start)]
    visited = set()
    came_from = {}
    cost = {start: 0}

    while heap:
        curr_cost, current = heapq.heappop(heap)
        if current == goal:
            return reconstruct_path(came_from, current)

        if current in visited:
            continue
        visited.add(current)

        for neighbor in neighbors(current, grid_size):
            if is_collision(neighbor, obstacle_positions):
                continue
            new_cost = curr_cost + 1
            if neighbor not in cost or new_cost < cost[neighbor]:
                cost[neighbor] = new_cost
                came_from[neighbor] = current
                heapq.heappush(heap, (new_cost, neighbor))

    return None

# 경로 역추적
def reconstruct_path(came_from, current):
    path = [current]
    while current in came_from:
        current = came_from[current]
        path.append(current)
    path.reverse()
    return path

# Fusion 알고리즘
def fusion_algorithm(start, trash_positions, obstacle_positions, env=None):
    grid_size = 50
    path = []
    current_pos = start
    collected = set()
    trash_set = set(trash_positions)

    max_iterations = 1000  # 최대 반복 수 (상황에 따라 조절)
    iteration = 0

    while trash_set - collected and iteration < max_iterations:
        iteration += 1
        # 1) Greedy: 가장 가까운 수집 대상 찾기
        remaining_trash = trash_set - collected
        target = min(remaining_trash, key=lambda t: distance(current_pos, t))

        # 2) A* 탐색
        route = a_star_search(current_pos, target, obstacle_positions, grid_size)
        
        if route is None:
            # A* 실패 → Dijkstra로 재탐색
            route = dijkstra_search(current_pos, target, obstacle_positions, grid_size)
        
        if route is None:
            # Dijkstra도 실패 → 다른 수집 대상 탐색
            alt_targets = [t for t in remaining_trash if t != target]
            found = False
            for alt in sorted(alt_targets, key=lambda t: distance(current_pos, t)):
                route = a_star_search(current_pos, alt, obstacle_positions, grid_size)
                if route is None:
                    route = dijkstra_search(current_pos, alt, obstacle_positions, grid_size)
                if route:
                    target = alt
                    found = True
                    break
            if not found:
                break  # 남은 쓰레기로 갈 수 있는 경로 없음

        # 3) 센싱 기반 간단한 경로 수정: 매 스텝마다 장애물 앞이면 우회
        for step in route[1:]:
            if is_collision(step, obstacle_positions):
                for neighbor in neighbors(current_pos, grid_size):
                    if not is_collision(neighbor, obstacle_positions):
                        step = neighbor
                        break
                else:
                    break  # 이동 불가한 경우 종료
            path.append(step)
            current_pos = step

            if current_pos in trash_set and current_pos not in collected:
                collected.add(current_pos)

    return path

# ================================================
# 알고리즘 성능 평가 함수
# ================================================
def evaluate_algorithm(algorithm, start, trash_positions, obstacle_positions, env=None):
    start_time = time.time()
    results = algorithm(start, trash_positions.copy(), obstacle_positions, env)
    end_time = time.time()

    collection_rate = len(set(results) & set(trash_positions)) / len(trash_positions) if trash_positions else 0
    collision_count = len([pos for pos in results if pos in obstacle_positions])
    search_distance = len(results)
    elapsed_time = end_time - start_time

    return {
        "collection_rate": collection_rate,
        "collision_count": collision_count,
        "search_distance": search_distance,
        "elapsed_time": elapsed_time
    }

# ================================================
# 결과 출력 함수
# ================================================
def print_results(results):
    for name, metrics in results.items():
        print(f"{name} Algorithm:")
        print(f"  수집률: {metrics['collection_rate']:.4f}")
        print(f"  충돌 횟수: {metrics['collision_count']}")
        print(f"  탐색 거리: {metrics['search_distance']}")
        print(f"  실행 시간: {metrics['elapsed_time']:.4f} seconds")
        print()

# ================================================
# 결과 저장 함수
# ================================================
save_folder = "algo_test_results"
os.makedirs(save_folder, exist_ok=True)

algorithm_names = ["❤️Fusion"]

def save_partial_results(all_results, save_count):
    rows = []
    for i, result in enumerate(all_results):
        for algo in algorithm_names:
            row = {
                "Index": i,
                "Algorithm": algo,
                "CollectionRate": result[algo]["collection_rate"],
                "CollisionCount": result[algo]["collision_count"],
                "SearchDistance": result[algo]["search_distance"],
                "ElapsedTime": result[algo]["elapsed_time"]
            }
            rows.append(row)
    df = pd.DataFrame(rows)
    df.to_csv(f"{save_folder}/algorithm_test_results_{save_count}.csv", index=False, encoding='utf-8-sig')

# ================================================
# 메인 실행
# ================================================
all_results = []
batch_size = 500
save_count = 1
total = len(data)

for index, row in data.iterrows():
    trash_positions, obstacle_positions = generate_trash_obstacles_with_environment(50, row)
    start = (0, 0)

    fusion_results = evaluate_algorithm(fusion_algorithm, start, trash_positions, obstacle_positions, env=row)

    results = {
        "❤️Fusion": fusion_results,
    }

    all_results.append(results)

    print()
    print("================================================")
    print(f"Data Index {index+1}/{total}: ({(index+1)/total*100:.2f}%)")
    print()
    print_results(results)

    if (index + 1) % batch_size == 0 or (index + 1) == total:
        save_partial_results(all_results, save_count)
        print(f"Results saved: batch {save_count}")
        save_count += 1
        all_results = []

if all_results:
    save_partial_results(all_results, save_count)
    print("\n================================================")
    print(f"✅ TEST RESULT FILE SAVED: algorithm_test_results_{save_count}.csv")
    print("================================================")
    print()

# ================================================
# 평균 결과 계산
# ================================================
print("\n================================================")
print("✅ TEST RESULT AVERAGE")
file_list = glob.glob(f"{save_folder}/algorithm_test_results_*.csv")

dfs = []
for file in file_list:
    dfs.append(pd.read_csv(file))

full_df = pd.concat(dfs, ignore_index=True)

for algo in algorithm_names:
    algo_df = full_df[full_df["Algorithm"] == algo]
    print(f"\n=== {algo} Algorithm ===")
    print(f"  수집률 평균: {algo_df['CollectionRate'].mean():.4f}")
    print(f"  충돌 횟수 평균: {algo_df['CollisionCount'].mean():.4f}")
    print(f"  탐색 거리 평균: {algo_df['SearchDistance'].mean():.4f}")
    print(f"  실행 시간 평균: {algo_df['ElapsedTime'].mean():.4f} seconds")

print()
